using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace T89CompilerLib.ScriptComponents
{
    public sealed class T89IncludesSection : T89ScriptSection
    {
        /// <summary>
        /// Internal list of includes
        /// </summary>
        public HashSet<ulong> Includes = new HashSet<ulong>();

        public T89ScriptObject Script { get; private set; }
        private T89IncludesSection(T89ScriptObject script) { Script = script; } //We dont want initializations of this class without our deserialization procedures

        public static T89IncludesSection New(T89ScriptObject script)
        {
            T89IncludesSection section = new T89IncludesSection(script);
            section.Includes = new HashSet<ulong>();
            return section;
        }

        /// <summary>
        /// Add an include to this script
        /// </summary>
        /// <param name="Include"></param>
        public void Add(ulong Include)
        {
            Includes.Add(Include);
        }

        /// <summary>
        /// Remove an include from this script
        /// </summary>
        /// <param name="Include"></param>
        public void Remove(ulong Include)
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

            foreach (ulong s in Includes)
                count += 8;

            uint Base = GetBaseAddress();

            count = (Base + count).AlignValue(0x10) - Base;

            return count;
        }

        public static void ReadIncludes(ref byte[] data, uint IncludesPosition, ushort NumIncludes, ref T89IncludesSection section, T89ScriptObject script)
        {
            T89IncludesSection includes = new T89IncludesSection(script);
            section = includes;

            if (NumIncludes < 1)
                return;

            if (data.Length <= IncludesPosition)
                throw new ArgumentException("GSC could not be parsed because the includes pointer was outside of boundaries of the input buffer.");

            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            reader.BaseStream.Position = IncludesPosition;

            for(byte i = 0; i < NumIncludes; i++)
            {
                includes.Includes.Add(reader.ReadUInt64());
            }

            reader.Dispose();
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[Size()];

            BinaryWriter writer = new BinaryWriter(new MemoryStream(data));

            foreach(ulong s in Includes)
                writer.Write(s);

            writer.Dispose();

            return data;
        }

        public override void UpdateHeader(ref T89ScriptHeader Header)
        {
            Header.IncludeCount = (byte)Count();
            Header.IncludeTableOffset = GetBaseAddress();
        }
    }
}
