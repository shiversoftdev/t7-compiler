using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using T7CompilerLib.OpCodes;

namespace T7CompilerLib.ScriptComponents
{
    public sealed class T7StringTableSection : T7ScriptSection
    {
        internal EndianType Endianess;
        private T7StringTableSection(bool littleEndian) { Endianess = littleEndian ? EndianType.LittleEndian : EndianType.BigEndian; } //Prevent public initializers

        public static T7StringTableSection New(bool littleEndian)
        {
            T7StringTableSection section = new T7StringTableSection(littleEndian);
            section.LoadedOffsetPairs = new Dictionary<uint, T7StringTableEntry>();
            section.TableEntries = new Dictionary<string, T7StringTableEntry>();
            return section;
        }

        /// <summary>
        /// This table is a store of the loaded offsets to the referenced string table entries. Its a reversed link that allows new OP_GetString codes to resolve their expected reference.
        /// </summary>
        public Dictionary<uint, T7StringTableEntry> LoadedOffsetPairs;

        internal Dictionary<string, T7StringTableEntry> TableEntries;

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
            
            uint CurrentString = 0;
            uint Base = GetBaseAddress();

            foreach (string s in TableEntries.Keys)
            {
                TableEntries[s].EmissionLocation = (uint)(Base + writer.BaseStream.Position);
                writer.WriteNullTerminatedString(TableEntries[s].Value);
            }

            writer.Dispose();
            return bytes;
        }

        public override uint Size()
        {
            uint count = 0;

            foreach (string s in TableEntries.Keys)
            {
                count += (uint)s.Length + 1;
            }

            uint Base = GetBaseAddress();

            count = (Base + count).AlignValue(0x10) - Base;

            return count;
        }

        public static void ReadStrings(ref byte[] data, bool littleEndian, uint lpStringTable, ushort NumStrings, ref T7StringTableSection Table)
        {
            Table = new T7StringTableSection(littleEndian);

            if (NumStrings < 1)
                return;

            if (lpStringTable >= data.Length)
                throw new ArgumentException("Couldn't read this gsc's strings because the string table pointer extends outside of the boundaries of the data supplied.");

            Table.LoadedOffsetPairs = new Dictionary<uint, T7StringTableEntry>();
            Table.TableEntries = new Dictionary<string, T7StringTableEntry>();

            EndianReader reader = new EndianReader(new MemoryStream(data), Table.Endianess);
            reader.BaseStream.Position = lpStringTable;

            //Note: The bytecode loader is in charge of updating and maintaining references to the string table.
            for (int i = 0; i < NumStrings; i++)
            {
                T7StringTableEntry entry = new T7StringTableEntry();
                uint StringPointer = (Table.Endianess == EndianType.LittleEndian) ? reader.ReadUInt32() : reader.ReadUInt16();

                if (StringPointer >= data.Length)
                    throw new ArgumentException("Couldn't read a string from the string table because the pointer exceeded the boundaries of the data supplied. May be an alignment issue.");

                entry.Value = reader.PeekNullTerminatedString(StringPointer, 256);

                ushort NumRefs = reader.ReadByte();
                reader.ReadByte();

                if (Table.Endianess == EndianType.LittleEndian)
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
        public T7StringTableEntry GetLoadedEntry(uint OpCodeAddy)
        {
            if (LoadedOffsetPairs.ContainsKey(OpCodeAddy))
                return LoadedOffsetPairs[OpCodeAddy];

            throw new ArgumentException("Couldn't resolve the string table entry for an opcode because the address requested was not expected.");
        }

        public override void UpdateHeader(ref T7ScriptHeader Header)
        {
        }

        /// <summary>
        /// Add a string to the string table
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T7StringTableEntry AddString(string value)
        {
            if (TableEntries.ContainsKey(value))
                return TableEntries[value];

            T7StringTableEntry entry = new T7StringTableEntry();

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
        public bool TryGetString(string value, out T7StringTableEntry entry)
        {
            return TableEntries.TryGetValue(value, out entry);
        }
    }

    public sealed class T7StringTableEntry
    {
        internal T7StringTableEntry() { }
        public HashSet<T7OP_GetString> References = new HashSet<T7OP_GetString>();
        public uint EmissionLocation;
        public string Value;

        public byte NumEntryEmissions()
        {
            return (byte)Math.Ceiling(References.Count / (float)250);
        }

        public uint[] CollectReferences()
        {
            List<uint> refs = new List<uint>();
            foreach (var _var in References)
            {
                refs.Add(_var.GetCommitDataAddress());
            }
            return refs.ToArray();
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
