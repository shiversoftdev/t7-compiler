using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreyarchCompiler.Games
{
    internal static class T6StringFixups
    {
        internal class XString
        {
            internal byte StringType; // only type 1 strings can be optimized (cannon). Non-cannon are imported to a diff table (includes, anims, stringlits, etc)
            internal ushort NamePtr;
            internal List<uint> References = new List<uint>();
            internal int NumberOfReferences
            {
                get
                {
                    return References.Count;
                }
            }
        }

        private const int OFFSET_STRTBL = 0x18;
        private const int OFFSET_STRCT = 0x32;
        private const int OFFSET_EXPTBL = 0x1C;
        private const int OFFSET_EXPCT = 0x34;
        /// <summary>
        /// Patches the string fixups section to optimize down the string count being used by the script
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="SafeCreateLocalVariables"></param>
        /// <returns>The number of strings that were saved in this optimization</returns>
        public static int PatchBuffer(byte[] raw, byte SafeCreateLocalVariables, bool IsLittleEndian)
        {
            // setup pointer information from the header
            var strData = raw.Skip(OFFSET_STRTBL).Take(sizeof(int));
            if (!IsLittleEndian) strData = strData.Reverse();
            var strCtData = raw.Skip(OFFSET_STRCT).Take(sizeof(short));
            if (!IsLittleEndian) strCtData = strCtData.Reverse();
            var expData = raw.Skip(OFFSET_EXPTBL).Take(sizeof(int));
            if (!IsLittleEndian) expData = expData.Reverse();
            var expCtData = raw.Skip(OFFSET_EXPCT).Take(sizeof(short));
            if (!IsLittleEndian) expCtData = expCtData.Reverse();

            int strOffset = BitConverter.ToInt32(strData.ToArray(), 0);
            int strCount = BitConverter.ToInt16(strCtData.ToArray(), 0);
            int exportsOffset = BitConverter.ToInt32(expData.ToArray(), 0);
            int exportsCount = BitConverter.ToInt16(expCtData.ToArray(), 0);
            Dictionary<int, XString> FixupTable = new Dictionary<int, XString>();
            Dictionary<uint, int> BackReference = new Dictionary<uint, int>();
            BinaryReader read = new BinaryReader(new MemoryStream(raw));
            read.BaseStream.Position = strOffset;
            
            // parse out the strings
            for(int i = 0; i < strCount; i++)
            {
                var spStrData = read.ReadBytes(2);
                if (!IsLittleEndian) spStrData = spStrData.Reverse().ToArray();

                var spStr = BitConverter.ToUInt16(spStrData, 0);
                var sbRefCt = read.ReadByte();
                var sbRefType = read.ReadByte();
                var dwKey = spStr & (sbRefType << 16);
                FixupTable[dwKey].StringType = sbRefType;
                if (!FixupTable.ContainsKey(dwKey))
                {
                    FixupTable[dwKey] = new XString();
                    FixupTable[dwKey].NamePtr = spStr;
                }

                for(int j = 0; j < sbRefCt; j++)
                {
                    var lpRefData = read.ReadBytes(4);
                    if (!IsLittleEndian) lpRefData = lpRefData.Reverse().ToArray();

                    var lpRef = BitConverter.ToUInt32(lpRefData, 0);
                    BackReference[lpRef] = dwKey;
                    FixupTable[dwKey].References.Add(lpRef);
                }
            }

            // cache string table size for later
            int strTblSize = (int)(read.BaseStream.Position - strOffset);

            // parse exports
            List<uint> ExportPointers = new List<uint>();
            read.BaseStream.Position = exportsOffset;
            for(int i = 0; i < exportsCount; i++)
            {
                read.BaseStream.Position += 4; // skip crc32
                var lpExportData = read.ReadBytes(4);
                if (!IsLittleEndian) lpExportData = lpExportData.Reverse().ToArray();
                var lpExport = BitConverter.ToUInt32(lpExportData, 0);
                ExportPointers.Add(lpExport);
                read.BaseStream.Position += 4; // skip other stuff we dont care about
            }

            int MaxNumLocals = 0;
            // remove references to local variables
            foreach(var lpExport in ExportPointers)
            {
                read.BaseStream.Position = lpExport;
                if (read.ReadByte() != SafeCreateLocalVariables) continue;
                var numVars = read.ReadByte();
                if (read.BaseStream.Position % 2 > 0) read.ReadByte();
                if(!BackReference.ContainsKey((uint)read.BaseStream.Position))
                {
                    throw new Exception("Failed to parse local variables for script optimization");
                }

                MaxNumLocals = Math.Max(MaxNumLocals, numVars);
                for (int i = 0; i < numVars; i++)
                {
                    // decrement the references to this variable, because we are going to purge this from the string table if we can, then consolidate after we remove all the unreferenced values
                    bool result = FixupTable[BackReference[(uint)read.BaseStream.Position]].References.Remove((uint)read.BaseStream.Position);
                    if (!result)
                    {
                        throw new Exception("Failed to remove a local variable for script optimization");
                    }
                    read.BaseStream.Position += 2;
                }
            }

            // purge any unused string entries
            var keys = FixupTable.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var value = FixupTable[key];
                if(value.References.Count == 0)
                {
                    i--;
                    FixupTable.Remove(key);
                }
            }

            // locate any suitable replacement candidates
            HashSet<XString> SuitableCandidates = new HashSet<XString>();
            foreach(var kvp in FixupTable)
            {
                if(kvp.Value.StringType == 0)
                {
                    continue;
                }
                SuitableCandidates.Add(kvp.Value);
            }

            // edge case: we are optimizing a script with so few cannonicals that removing locals produces an unsuitable situation for a function with too many locals
            if(SuitableCandidates.Count < MaxNumLocals)
            {
                // this script doesnt need optimization
                return 0;
            }

            // optimize the locals cache of each function
            List<XString> candidates = SuitableCandidates.ToList();
            foreach (var lpExport in ExportPointers)
            {
                read.BaseStream.Position = lpExport;
                if (read.ReadByte() != SafeCreateLocalVariables) continue;
                var numVars = read.ReadByte();
                if (read.BaseStream.Position % 2 > 0) read.ReadByte();
                
                for(int i = 0; i < numVars; i++)
                {
                    candidates[i].References.Add((uint)read.BaseStream.Position);
                    read.BaseStream.Position += 2;
                }
            }

            read.Close();

            // wipe the old fixup table
            new byte[strTblSize].CopyTo(raw, strOffset);

            BinaryWriter write = new BinaryWriter(new MemoryStream(raw));
            write.BaseStream.Position = strOffset;

            // serialize the new fixup table
            int numEntriesEmitted = 0;
            foreach (var kvp in FixupTable)
            {
                var fixup = kvp.Value;
                var namePtr = BitConverter.GetBytes(kvp.Value.NamePtr);
                if (!IsLittleEndian) namePtr = namePtr.Reverse().ToArray();

                int index = 0;
                var strrefs = fixup.References.ToArray();

                while (index < strrefs.Length)
                {
                    if (index % 250 == 0)
                    {
                        numEntriesEmitted++;
                        write.Write(namePtr);
                        write.Write((byte)Math.Min(strrefs.Length - index, 250));
                        write.Write(fixup.StringType);
                    }

                    var valueBytes = BitConverter.GetBytes(strrefs[index++]);
                    if (!IsLittleEndian) valueBytes = valueBytes.Reverse().ToArray();
                    write.Write(valueBytes);
                }
            }

            write.Close();

            // emit the new string entry count
            var numEntriesData = BitConverter.GetBytes((ushort)numEntriesEmitted);
            if (!IsLittleEndian) numEntriesData = numEntriesData.Reverse().ToArray();
            numEntriesData.CopyTo(raw, OFFSET_STRCT);

            return (ushort)(strCount - numEntriesEmitted);
        }
    }
}
