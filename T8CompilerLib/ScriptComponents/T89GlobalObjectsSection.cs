using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T89CompilerLib.OpCodes;

namespace T89CompilerLib.ScriptComponents
{
    public class T89GlobalObjectsSection : T89ScriptSection
    {
        private Dictionary<uint, T89GlobalRef> ObjectTable = new Dictionary<uint, T89GlobalRef>();
        public T89ScriptObject Script { get; private set; }
        private T89GlobalObjectsSection(T89ScriptObject script)
        {
            Script = script;
        } //Prevent public initializers

        internal static T89GlobalObjectsSection New(T89ScriptObject script)
        {
            return new T89GlobalObjectsSection(script);
        }

        public override ushort Count()
        {
            return (ushort)ObjectTable.Count;
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[Size()];
            BinaryWriter writer = new BinaryWriter(new MemoryStream(data));
            foreach(var entry in ObjectTable)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value.References.Count);
                foreach (var val in entry.Value.References) writer.Write(val.GetCommitDataAddress());
            }
            writer.Close();
            return data;
        }

        public override uint Size()
        {
            int count = 0;
            foreach (var entry in ObjectTable) count += 8 + (entry.Value.References.Count * 4);
            return (uint)count;
        }

        public override void UpdateHeader(ref T89ScriptHeader Header)
        {
            Header.GlobalObjectCount = Count();
            Header.GlobalObjectTable = GetBaseAddress();
        }

        public T89GlobalRef AddGlobal(uint value)
        {
            if (!ObjectTable.ContainsKey(value)) ObjectTable[value] = new T89GlobalRef();
            return ObjectTable[value];
        }
    }
    public class T89GlobalRef
    {
        internal HashSet<T89OP_GetGlobal> References = new HashSet<T89OP_GetGlobal>();
    }
}
