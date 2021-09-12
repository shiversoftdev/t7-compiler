using System;
using System.Collections.Generic;
using System.IO;
using T89CompilerLib.OpCodes;

namespace T89CompilerLib.ScriptComponents
{
    public sealed class T89StringTableSection : T89ScriptSection
    {
        public T89ScriptObject Script { get; private set; }
        private T89StringTableSection(T89ScriptObject script) { Script = script; } //Prevent public initializers

        public static T89StringTableSection New(T89ScriptObject script)
        {
            T89StringTableSection section = new T89StringTableSection(script);
            section.LoadedOffsetPairs = new Dictionary<uint, T89StringTableEntry>();
            section.TableEntries = new Dictionary<string, T89StringTableEntry>();
            return section;
        }

        /// <summary>
        /// This table is a store of the loaded offsets to the referenced string table entries. Its a reversed link that allows new OP_GetString codes to resolve their expected reference.
        /// </summary>
        private Dictionary<uint, T89StringTableEntry> LoadedOffsetPairs;

        private Dictionary<string, T89StringTableEntry> TableEntries;

        public override ushort Count()
        {
            return (ushort)TableEntries.Keys.Count;
        }

        public override byte[] Serialize()
        {
            byte[] bytes = new byte[Size()];
            BinaryWriter writer = new BinaryWriter(new MemoryStream(bytes));
            
            uint CurrentString = 0;
            uint Base = GetBaseAddress();

            foreach (string s in TableEntries.Keys)
            {
                CurrentString += ((uint)TableEntries[s].References.Count * 4) + 8;
            }

            //TODO: dupe entry writer. they didnt increase to a short in this game because that makes too much sense, so we have to do what we did in bo2.
            foreach (string s in TableEntries.Keys)
            {
                writer.Write(CurrentString + Base); //4
                writer.Write((byte)TableEntries[s].References.Count); //1
                writer.Write((byte)0); //1
                writer.Write((ushort)0); //2

                foreach (var reference in TableEntries[s].References)
                {
                    writer.Write(reference.GetCommitDataAddress());
                }

                uint CachedLocation = (uint)writer.BaseStream.Position;
                writer.BaseStream.Position = CurrentString;

                writer.WriteNullTerminatedString(TableEntries[s].Value);

                CurrentString = (uint)writer.BaseStream.Position;

                writer.BaseStream.Position = CachedLocation;
            }
            
            writer.Dispose();

            return bytes;
        }

        public override uint Size()
        {
            uint count = 0;
            //TODO mod this to work with dupe emitter
            foreach(string s in TableEntries.Keys)
            {
                count += (uint)s.Length + 1 + 8 + 2; //null terminated + header + prefix
                count += (uint)TableEntries[s].References.Count * 4; //uint * count
            }

            uint Base = GetBaseAddress();

            count = (Base + count).AlignValue(0x10) - Base;

            return count;
        }

        public static void ReadStrings(ref byte[] data, uint lpStringTable, ushort NumStrings, ref T89StringTableSection Table, T89ScriptObject script)
        {
            Table = new T89StringTableSection(script);

            if (NumStrings < 1)
                return;

            if (lpStringTable >= data.Length)
                throw new ArgumentException("Couldn't read this gsc's strings because the string table pointer extends outside of the boundaries of the data supplied.");

            Table.LoadedOffsetPairs = new Dictionary<uint, T89StringTableEntry>();
            Table.TableEntries = new Dictionary<string, T89StringTableEntry>();

            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            reader.BaseStream.Position = lpStringTable;

            //Note: The bytecode loader is in charge of updating and maintaining references to the string table.
            for (int i = 0; i < NumStrings; i++)
            {
                T89StringTableEntry entry = new T89StringTableEntry();
                uint StringPointer = reader.ReadUInt32();

                if (StringPointer >= data.Length)
                    throw new ArgumentException("Couldn't read a string from the string table because the pointer exceeded the boundaries of the data supplied. May be an alignment issue.");

                //Skip encryption prefix
                if (data[StringPointer] == 0x9F)
                    StringPointer += 2;
                
                entry.Value = reader.PeekNullTerminatedString(StringPointer, 255);

                ushort NumRefs = reader.ReadUInt16();
                reader.ReadUInt16();

                for(ushort j = 0; j < NumRefs; j++)
                {
                    Table.LoadedOffsetPairs[reader.ReadUInt32()] = entry;
                }

                Table.TableEntries[entry.Value] = entry;
            }

            reader.Dispose();
        }

        /// <summary>
        /// Retrieve a loaded entry from the string table when loading the bytecode section
        /// </summary>
        /// <param name="OpCodeAddy"></param>
        /// <returns></returns>
        public T89StringTableEntry GetLoadedEntry(uint OpCodeAddy)
        {
            if (LoadedOffsetPairs.ContainsKey(OpCodeAddy))
                return LoadedOffsetPairs[OpCodeAddy];

            throw new ArgumentException("Couldn't resolve the string table entry for an opcode because the address requested was not expected.");
        }

        public override void UpdateHeader(ref T89ScriptHeader Header)
        {
            Header.StringCount = Count();
            Header.StringTableOffset = GetBaseAddress();
        }

        /// <summary>
        /// Add a string to the string table
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T89StringTableEntry AddString(string value)
        {
            if (TableEntries.ContainsKey(value))
                return TableEntries[value];

            T89StringTableEntry entry = new T89StringTableEntry();

            entry.Value = value;
            TableEntries[value] = entry;

            return entry;
        }

        /// <summary>
        /// Try to get a string from the string table
        /// </summary>
        /// <param name="value"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool TryGetString(string value, out T89StringTableEntry entry)
        {
            return TableEntries.TryGetValue(value, out entry);
        }
    }

    public sealed class T89StringTableEntry
    {
        internal T89StringTableEntry() { }
        public HashSet<T89OP_GetString> References = new HashSet<T89OP_GetString>();
        public string Value;
    }
}
