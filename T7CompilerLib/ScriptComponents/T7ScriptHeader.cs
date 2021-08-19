using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T7CompilerLib.OpCodes;

namespace T7CompilerLib.ScriptComponents
{
    /// <summary>
    /// Script header for T7 scripts
    /// </summary>
    public sealed class T7ScriptHeader : T7ScriptSection
    {
        private T7ScriptHeader(bool littleEndian) 
        { 
            Endianess = littleEndian ? EndianType.LittleEndian : EndianType.BigEndian;
            
        } //We dont want initialization outside of our internal method

        private EndianType Endianess;

        const byte HEADER_SIZE = 0x50;

        public ulong ScriptMagic { get; private set; }

        public uint SourceChecksum { get; internal set; }
        public uint IncludeTableOffset { get; internal set; }
        public uint AnimTreeTableOffset { get; internal set; }
        public uint ByteCodeOffset { get; internal set; }
        public uint StringTableOffset { get; internal set; }
        public uint DebugStringTableOffset { get; internal set; }
        public uint ExportTableOffset { get; internal set; }
        public uint ImportTableOffset { get; internal set; }
        public uint FixupTableOffset { get; internal set; }
        public uint ProfileTableOffset { get; internal set; }


        public uint ByteCodeSize { get; internal set; }
        public uint NameOffset { get; internal set; }
        public ushort StringCount { get; internal set; }
        public ushort ExportsCount { get; internal set; }
        public ushort ImportsCount { get; internal set; }
        public ushort FixupCount { get; internal set; }
        public ushort ProfileCount { get; internal set; }
        public ushort DebugStringCount { get; internal set; }
        public byte IncludeCount { get; internal set; }
        public byte AnimTreeCount { get; internal set; }
        public byte Flags { get; internal set; }

        private void Deserialize(ref byte[] data, ulong ExpectedMagic)
        {

            if (data.Length < HEADER_SIZE)
                throw new ArgumentException("Provided GSC file is not a valid T7 script; data is too short");

            EndianReader reader = new EndianReader(new MemoryStream(data), Endianess); //TODO: if an invalid magic is passed, this resource will leak.

            ulong Magic = reader.ReadUInt64();

            if (Magic != ExpectedMagic)
                throw new ArgumentException("Provided GSC file is not a valid T7 script; invalid magic");

            SourceChecksum = reader.ReadUInt32();
            IncludeTableOffset = reader.ReadUInt32();

            AnimTreeTableOffset = reader.ReadUInt32();
            ByteCodeOffset = reader.ReadUInt32();
            StringTableOffset = reader.ReadUInt32();
            DebugStringTableOffset = reader.ReadUInt32();

            ExportTableOffset = reader.ReadUInt32();
            ImportTableOffset = reader.ReadUInt32();
            FixupTableOffset = reader.ReadUInt32();
            ProfileTableOffset = reader.ReadUInt32();

            ByteCodeSize = reader.ReadUInt32();
            NameOffset = (Endianess == EndianType.LittleEndian) ? reader.ReadUInt32() : reader.ReadUInt16(); //alignment fix for xbox
            StringCount = reader.ReadUInt16();
            ExportsCount = reader.ReadUInt16();
            ImportsCount = reader.ReadUInt16();
            FixupCount = reader.ReadUInt16();

            ProfileCount = reader.ReadUInt16();
            DebugStringCount = reader.ReadUInt16();
            IncludeCount = reader.ReadByte();
            AnimTreeCount = reader.ReadByte();
            Flags = reader.ReadByte();

            reader.Dispose();
        }

        public int LowestSectionPtrAfter(int offset)
        {
            int Lowest = 0x7FFFFFFF;

            List<int> Sections = new int[]
            {
                (int)IncludeTableOffset,
                (int)AnimTreeTableOffset,
                (int)ByteCodeOffset,
                (int)StringTableOffset,
                (int)DebugStringTableOffset,
                (int)ExportTableOffset,
                (int)ImportTableOffset,
                (int)FixupTableOffset,
                (int)ProfileTableOffset
            }.ToList();

            return Sections.Where(x => x > offset).OrderByDescending(x => x).Last();
        }

        public override byte[] Serialize()
        {
            byte[] header = new byte[HEADER_SIZE];
            CommitHeader(ref header, ScriptMagic);
            return header;
        }

        /// <summary>
        /// Read a header from a data array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outputRef"></param>
        public static void ReadHeader(ref byte[] data, bool littleEndian, ref T7ScriptHeader outputRef, ulong ExpectedMagic)
        {
            outputRef = new T7ScriptHeader(littleEndian);
            outputRef.Deserialize(ref data, ExpectedMagic);
        }

        public void CommitHeader(ref byte[] raw, ulong magic)
        {
            EndianWriter writer = new EndianWriter(new MemoryStream(raw), Endianess);
            ScriptMagic = magic;

            writer.Write(ScriptMagic);           //0x0
            writer.Write(SourceChecksum);
            writer.Write(IncludeTableOffset);
            
            writer.Write(raw.Length); //unsupported 0x10
            writer.Write(ByteCodeOffset);
            writer.Write(StringTableOffset);
            writer.Write(DebugStringTableOffset);

            writer.Write(ExportTableOffset); //0x20
            writer.Write(ImportTableOffset);
            writer.Write(raw.Length); //unsupported
            writer.Write(raw.Length); //unsupported

            writer.Write(ByteCodeSize); //0x30
            if(Endianess == EndianType.LittleEndian)
                writer.Write(NameOffset);//0xFFFFFFFFu
            else
                writer.Write((ushort)NameOffset);
            writer.Write(StringCount);
            writer.Write(ExportsCount);
            writer.Write(ImportsCount);
            writer.Write((ushort)0); //unsupported
            writer.Write((ushort)0); //unsupported

            writer.Write(DebugStringCount); //0x40
            writer.Write(IncludeCount);
            writer.Write((byte)0); //unsupported
            writer.Write(Flags);

            writer.Dispose();
        }

        public override uint Size()
        {
            return HEADER_SIZE;
        }

        public override ushort Count()
        {
            return 1;
        }

        public override void UpdateHeader(ref T7ScriptHeader header) { }

        public static T7ScriptHeader New(bool littleEndian)
        {
            T7ScriptHeader header = new T7ScriptHeader(littleEndian);
            header.SourceChecksum = 0x4c492053;
            return header;
        }
    }
}