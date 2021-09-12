using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static T89CompilerLib.VMREVISIONS;
//note: scripts/zm/_zm_clone = 661D141FAF0648BA
//note: function calls now have a word after the opcode
namespace T89CompilerLib.ScriptComponents
{
    /// <summary>
    /// Script header for T89 scripts
    /// </summary>
    public sealed class T89ScriptHeader : T89ScriptSection
    {
        public T89ScriptObject Script { get; private set; }
        private T89ScriptHeader(T89ScriptObject script) { Script = script; } //We dont want initialization outside of our internal method

        public byte HEADER_SIZE
        {
            get
            {
                switch (Script.VM)
                {
                    case VM_36:
                        return 0x60;
                }
                return 0x80;
            }
        }

        public ulong ScriptMagic => BitConverter.ToUInt64(new byte[] { 0x80, 0x47, 0x53, 0x43, 0x0D, 0x0A, 0x00, (byte) Script.VM}, 0); //0x0,0x8
        public uint SourceChecksum { get; internal set; } //0x8, 0xC
        public uint UNK_0C { get; internal set; } //0xC, 0x10

        public ulong ScriptName { get; set; } //0x10, 0x18
        public uint IncludeTableOffset { get; internal set; } //0x18, 0x1C
        public ushort StringCount { get; internal set; } //0x1C, 0x1E
        public ushort ExportsCount { get; internal set; } //0x1E, 0x20

        public uint UNK_20 { get; internal set; } //0x20, 0x24 -- probably devstrings
        public uint StringTableOffset { get; internal set; } //0x24, 0x28
        public ushort ImportsCount { get; internal set; } //0x28, 0x2A
        public ushort FixupCount { get; internal set; } //0x2A, 0x2C
        public uint UNK_2C { get; internal set; } //0x2C, 0x30

        public uint ExportTableOffset { get; internal set; }//0x30, 0x34
        public uint UNK_34 { get; internal set; } //0x34, 0x38
        public uint ImportTableOffset { get; internal set; } //0x38, 0x3C
        public ushort GlobalObjectCount { get; internal set; } //VM_36{0x3C, 0x3E}, VM_37{0x38,0x3A}
        public ushort UNK_3E { get; internal set; } //0x3E, 0x40

        public uint UNK_40 { get; internal set; } //0x40, 0x44
        public uint GlobalObjectTable { get; internal set; } //0x44, 0x48
        public uint UNK_48 { get; internal set; } //0x48, 0x4C
        public uint UNK_4C { get; internal set; } //0x4C, 0x50

        public uint UNK_50 { get; internal set; } //0x50, 0x54
        public uint UNK_54 { get; internal set; } //0x54, 0x58
        public ushort IncludeCount { get; internal set; } //0x58, 0x5A
        public ushort UNK_5A { get; internal set; } //0x5A, 0x5C
        public uint UNK_5C { get; internal set; } //0x5C, 0x60

        private void Deserialize(ref byte[] data, ulong ExpectedMagic)
        {
            if (data.Length < HEADER_SIZE)
                throw new ArgumentException("Provided GSC file is not a valid T89 script; data is too short");

            BinaryReader reader = new BinaryReader(new MemoryStream(data)); //TODO: if an invalid magic is passed, this resource will leak.

            ulong Magic = reader.ReadUInt64();

            if (Magic != ScriptMagic)
                throw new ArgumentException("Provided GSC file is not a valid T89 script; invalid magic");

            SourceChecksum = reader.ReadUInt32();
            UNK_0C = reader.ReadUInt32();

            //0x10
            ScriptName = reader.ReadUInt64();
            IncludeTableOffset = reader.ReadUInt32();
            StringCount = reader.ReadUInt16();
            ExportsCount = reader.ReadUInt16(); // 0x1E

            UNK_20 = reader.ReadUInt32();
            StringTableOffset = reader.ReadUInt32();
            ImportsCount = reader.ReadUInt16();
            FixupCount = reader.ReadUInt16();
            UNK_2C = reader.ReadUInt32();

            // 0x30
            ExportTableOffset = reader.ReadUInt32();
            if (Script.VM == VM_36) UNK_34 = reader.ReadUInt32(); // 0x34 / 0x30
            ImportTableOffset = reader.ReadUInt32(); // 0x38 / 0x34
            GlobalObjectCount = reader.ReadUInt16(); // 0x3C / 0x38
            UNK_3E = reader.ReadUInt16(); // 0x3E / 0x3A

            UNK_40 = reader.ReadUInt32(); // 0x40 / 0x3C
            GlobalObjectTable = reader.ReadUInt32(); // 0x44 / 0x40
            UNK_48 = reader.ReadUInt32();

            if (Script.VM == VM_36) UNK_4C = reader.ReadUInt32();

            UNK_50 = reader.ReadUInt32();
            UNK_54 = reader.ReadUInt32();
            IncludeCount = reader.ReadUInt16();
            UNK_5A = reader.ReadUInt16();
            UNK_5C = reader.ReadUInt32();

            reader.Dispose();
        }

        public override byte[] Serialize()
        {
            byte[] header = new byte[HEADER_SIZE];
            CommitHeader(ref header, ScriptMagic);
            return header;
        }

        public int LowestSectionPtrAfter(int offset)
        {
            int Lowest = 0x7FFFFFFF;

            List<int> Sections = new int[]
            {
                (int)IncludeTableOffset,
                (int)StringTableOffset,
                (int)ExportTableOffset,
                (int)ImportTableOffset,
                (int)GlobalObjectTable,
            }.ToList();

            return Sections.Where(x => x > offset).OrderByDescending(x => x).Last();
        }

        /// <summary>
        /// Read a header from a data array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outputRef"></param>
        public static void ReadHeader(ref byte[] data, ref T89ScriptHeader outputRef, ulong ExpectedMagic, T89ScriptObject script)
        {
            outputRef = new T89ScriptHeader(script);
            outputRef.Deserialize(ref data, ExpectedMagic);
        }

        public void CommitHeader(ref byte[] raw, ulong magic)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(raw));

            writer.Write(ScriptMagic);           //0x0
            writer.Write(SourceChecksum);
            writer.Write(UNK_0C);

            writer.Write(ScriptName);           //0x10
            writer.Write(IncludeTableOffset);
            writer.Write(StringCount);
            writer.Write(ExportsCount);

            writer.Write(UNK_20);               //0x20
            writer.Write(StringTableOffset);
            writer.Write(ImportsCount);
            writer.Write((ushort)0); //numfixups, unsupported
            writer.Write(UNK_2C);

            writer.Write(ExportTableOffset); //0x30
            if (Script.VM == VM_36) writer.Write(UNK_34);
            writer.Write(ImportTableOffset);
            writer.Write((ushort)GlobalObjectCount);
            writer.Write(UNK_3E);

            writer.Write(raw.Length); //0x40
            writer.Write(GlobalObjectTable); //unsupported
            writer.Write(raw.Length); //unsupported
            if (Script.VM == VM_36) writer.Write(raw.Length); //unsupported

            writer.Write(raw.Length); //0x50
            writer.Write(raw.Length); //unsupported
            writer.Write(IncludeCount);
            writer.Write((ushort)0);
            writer.Write((int)0);

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

        public override void UpdateHeader(ref T89ScriptHeader header) { }

        public static T89ScriptHeader New(T89ScriptObject script)
        {
            T89ScriptHeader header = new T89ScriptHeader(script);
            switch(script.VM)
            {
                case VM_36:
                    header.SourceChecksum = BitConverter.ToUInt32(new byte[] { 0x38, 0x9D, 0x6E, 0x63 }, 0);
                    break;
            }
            return header;
        }
    }
}
