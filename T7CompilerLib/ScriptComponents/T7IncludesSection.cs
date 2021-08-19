using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.ScriptComponents
{
    public sealed class T7IncludesSection : T7ScriptSection
    {
        private const int MAX_INCLUDE_SIZE = 128;
        private EndianType Endianess;
        /// <summary>
        /// Internal list of includes
        /// </summary>
        private HashSet<string> Includes = new HashSet<string>();

        private T7IncludesSection(bool littleEndian) { Endianess = littleEndian ? EndianType.LittleEndian : EndianType.BigEndian; } //We dont want initializations of this class without our deserialization procedures

        public static T7IncludesSection New(bool littleEndian)
        {
            T7IncludesSection section = new T7IncludesSection(littleEndian);
            section.Includes = new HashSet<string>();
            return section;
        }

        /// <summary>
        /// Add an include to this script
        /// </summary>
        /// <param name="Include"></param>
        public void Add(string Include)
        {
            Includes.Add(Include);
        }

        /// <summary>
        /// Remove an include from this script
        /// </summary>
        /// <param name="Include"></param>
        public void Remove(string Include)
        {
            Includes.Remove(Include);
        }

        /// <summary>
        /// Number of includes in this section
        /// </summary>
        /// <returns></returns>
        public override ushort Count()
        {
            return (ushort)Includes.Count;
        }

        /// <summary>
        /// Returns the section size. For includes, this consists of the string and the reference for each include.
        /// </summary>
        /// <returns></returns>
        public override uint Size()
        {
            uint count = 0;

            foreach (string s in Includes)
                count += (uint)s.Length + 4 + 1;

            uint Base = GetBaseAddress();

            count = (Base + count).AlignValue(0x10) - Base;

            return count;
        }

        public static void ReadIncludes(ref byte[] data, bool littleEndian, uint IncludesPosition, byte NumIncludes, ref T7IncludesSection section)
        {
            T7IncludesSection includes = new T7IncludesSection(littleEndian);
            section = includes;

            if (NumIncludes < 1)
                return;

            if (data.Length <= IncludesPosition)
                throw new ArgumentException("GSC could not be parsed because the includes pointer was outside of boundaries of the input buffer.");

            EndianReader reader = new EndianReader(new MemoryStream(data), includes.Endianess);
            reader.BaseStream.Position = IncludesPosition;

            for(byte i = 0; i < NumIncludes; i++)
            {
                uint StringOffset = reader.ReadUInt32();
                if (StringOffset >= data.Length)
                    continue; //Skipped a bad include. Should probably log this.

                includes.Includes.Add(reader.PeekNullTerminatedString(StringOffset, MAX_INCLUDE_SIZE));
            }

            reader.Dispose();
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[Size()];

            uint Base = GetBaseAddress();

            EndianWriter writer = new EndianWriter(new MemoryStream(data), Endianess);

            uint CurrentEmitLocation = (uint)Includes.Count * 4;

            int i = 0;
            foreach(string s in Includes)
            {
                byte[] StringData = Encoding.ASCII.GetBytes(s.Replace("\\","/")); //yall.... i just... why.

                writer.BaseStream.Position = i * 4;
                writer.Write(CurrentEmitLocation + Base);
                writer.BaseStream.Position = CurrentEmitLocation;
                writer.Write(StringData);
                writer.Write((byte)0x0);
                CurrentEmitLocation += (uint)StringData.Length + 1;

                i++;
            }

            writer.Dispose();

            return data;
        }

        public override void UpdateHeader(ref T7ScriptHeader Header)
        {
            Header.IncludeCount = (byte)Count();
            Header.IncludeTableOffset = GetBaseAddress();
        }
    }
}
