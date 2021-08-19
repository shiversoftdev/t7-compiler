using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using T7CompilerLib.OpCodes;

namespace T7CompilerLib.ScriptComponents
{
    public sealed class T7DebugTableSection : T7ScriptSection
    {
        private EndianType Endianess;
        private T7DebugTableSection(bool littleEndian) { Endianess = littleEndian ? EndianType.LittleEndian : EndianType.BigEndian; } //Prevent public initializers
        const int STRING_MAXSIZE = 256;

        public static T7DebugTableSection New(bool littleEndian)
        {
            T7DebugTableSection section = new T7DebugTableSection(littleEndian);
            section.LoadedOffsetPairs = new Dictionary<uint, T7DebugTableEntry>();
            section.TableEntries = new Dictionary<uint, T7DebugTableEntry>();
            return section;
        }

        /// <summary>
        /// This table is a store of the loaded offsets to the referenced string table entries. Its a reversed link that allows new OP_GetString codes to resolve their expected reference.
        /// </summary>
        public Dictionary<uint, T7DebugTableEntry> LoadedOffsetPairs;

        private Dictionary<uint, T7DebugTableEntry> TableEntries;

        public override ushort Count()
        {
            return (ushort)TableEntries.Keys.Count;
        }

        public IEnumerable<uint> LoadOffsets()
        {
            foreach (var entry in LoadedOffsetPairs.Keys)
                yield return entry;
        }

        public override byte[] Serialize()
        {
            byte[] bytes = new byte[Size()];
            EndianWriter writer = new EndianWriter(new MemoryStream(bytes), Endianess);

            foreach (uint u in TableEntries.Keys)
            {
                writer.Write(u);
                writer.Write((byte)TableEntries[u].References.Count);
                writer.Write((ushort)0);
                writer.Write((byte)0);

                foreach(var reference in TableEntries[u].References)
                {
                    writer.Write(reference.GetCommitDataAddress());
                }
            }

            writer.Dispose();

            return bytes;
        }

        public override uint Size()
        {
            uint count = 0;

            foreach(uint u in TableEntries.Keys)
            {
                count += 8;
                count += (uint)TableEntries[u].References.Count * 4;
            }

            uint Base = GetBaseAddress();

            count = (Base + count).AlignValue(0x10) - Base;

            return count;
        }

        public static void ReadStrings(ref byte[] data, bool littleEndian, uint lpStringTable, ushort NumStrings, ref T7DebugTableSection Table)
        {
            Table = new T7DebugTableSection(littleEndian);

            if (NumStrings < 1)
                return;

            if (lpStringTable >= data.Length)
                throw new ArgumentException("Couldn't read this gsc's debug strings because the debug table pointer extends outside of the boundaries of the data supplied.");

            Table.LoadedOffsetPairs = new Dictionary<uint, T7DebugTableEntry>();
            Table.TableEntries = new Dictionary<uint, T7DebugTableEntry>();

            EndianReader reader = new EndianReader(new MemoryStream(data), Table.Endianess);
            reader.BaseStream.Position = lpStringTable;

            //Note: The bytecode loader is in charge of updating and maintaining references to the debug table.
            for (int i = 0; i < NumStrings; i++)
            {
                T7DebugTableEntry entry = new T7DebugTableEntry();
                uint value = reader.ReadUInt32();

                entry.Value = value;

                byte NumRefs = reader.ReadByte();
                reader.ReadUInt16();
                reader.ReadByte();

                for (ushort j = 0; j < NumRefs; j++)
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
        public T7DebugTableEntry GetLoadedEntry(uint OpCodeAddy)
        {
            if (LoadedOffsetPairs.ContainsKey(OpCodeAddy))
                return LoadedOffsetPairs[OpCodeAddy];

            throw new ArgumentException("Couldn't resolve the debug table entry for an opcode because the address requested was not expected.");
        }

        public override void UpdateHeader(ref T7ScriptHeader Header)
        {
            Header.DebugStringCount = Count();
            Header.DebugStringTableOffset = GetBaseAddress();
        }
    }

    public sealed class T7DebugTableEntry
    {
        public HashSet<T7OP_GetString> References = new HashSet<T7OP_GetString>();
        public uint Value;

        public override string ToString()
        {
            return Value.ToString("X8");
        }
    }
}
