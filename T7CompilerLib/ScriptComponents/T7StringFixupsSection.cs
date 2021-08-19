using System;
using System.IO;

namespace T7CompilerLib.ScriptComponents
{
    public sealed class T7StringFixupsSection : T7ScriptSection
    {
        private T7StringTableSection StringTable;
        private int SIZEOF_FIXUP
        {
            get
            {
                if (StringTable.Endianess == EndianType.LittleEndian)
                    return 8;

                return 4;
            }
        }

        private T7StringFixupsSection() { }
        public static T7StringFixupsSection New(T7StringTableSection strings)
        {
            T7StringFixupsSection fixups = new T7StringFixupsSection();
            fixups.StringTable = strings;
            return fixups;
        }

        public override ushort Count()
        {
            int count = 0;
            foreach(var KvP in StringTable.TableEntries)
            {
                count += KvP.Value.NumEntryEmissions();
            }
            return (ushort)count;
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[Size()];

            EndianWriter writer = new EndianWriter(data, StringTable.Endianess);

            foreach (var KvP in StringTable.TableEntries)
            {
                int index = 0;
                uint[] strrefs = KvP.Value.CollectReferences();

                while (index < strrefs.Length)
                {
                    if(index % 250 == 0)
                    {
                        if (StringTable.Endianess == EndianType.LittleEndian)
                            writer.Write(KvP.Value.EmissionLocation);
                        else
                            writer.Write((ushort)KvP.Value.EmissionLocation);

                        writer.Write((byte)Math.Min(strrefs.Length - index, 250));
                        writer.Write((byte)0);
                        
                        if (StringTable.Endianess == EndianType.LittleEndian)
                            writer.Write((ushort)0);
                    }
                    writer.Write(strrefs[index++]);
                }
            }
            writer.Dispose();

            return data;
        }

        public override uint Size()
        {
            uint count = 0;
            foreach (var KvP in StringTable.TableEntries)
            {
                count += (uint)((KvP.Value.NumEntryEmissions() * SIZEOF_FIXUP) + (KvP.Value.CollectReferences().Length * sizeof(int)));
            }

            count = count.AlignValue(0x10); //meet section alignment requirements
            return count;
        }

        public override void UpdateHeader(ref T7ScriptHeader Header)
        {
            Header.StringCount = Count();
            Header.StringTableOffset = GetBaseAddress();
        }
    }
}
