using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.ScriptComponents
{
    public sealed class T7NameSection : T7ScriptSection
    {
        const int SCRIPT_NAME_MAXSIZE = 256;
        private EndianType Endianess;
        private T7NameSection(bool littleEndian) { Endianess = littleEndian ? EndianType.LittleEndian : EndianType.BigEndian; } //Dont want initializers

        public static T7NameSection New(bool littleEndian)
        {
            return new T7NameSection(littleEndian);
        }

        /// <summary>
        /// The name of this script
        /// </summary>
        public string Value { get; set; }

        public override ushort Count()
        {
            return 1;
        }

        public override uint Size()
        {
            uint Base = GetBaseAddress();
            return (ushort)(((uint)(Base + (Value.Length + 1))).AlignValue(0x10) - Base);
        }

        public static void ReadNameSection(ref byte[] raw, bool littleEndian, uint lpName, ref T7NameSection section)
        {
            section = new T7NameSection(littleEndian);

            if (raw.Length <= lpName)
                throw new ArgumentException("Couldn't parse the name pointer for this gsc because the pointer extends outside of the bounds of the input buffer.");

            EndianReader reader = new EndianReader(new MemoryStream(raw), section.Endianess);

            section.Value = reader.PeekNullTerminatedString(lpName, SCRIPT_NAME_MAXSIZE);

            reader.Dispose();
        }

        public override byte[] Serialize()
        {
            byte[] bytes = new byte[Size()];

            Encoding.ASCII.GetBytes(Value).CopyTo(bytes, 0);

            return bytes;
        }
        public override void UpdateHeader(ref T7ScriptHeader Header)
        {
            Header.NameOffset = GetBaseAddress();
        }
    }
}
