using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_Marker : T7OpCode
    {
        public string Identifier { get; private set; }
        public T7OP_Marker(string identifier, EndianType endianess) : base(ScriptOpCode.Nop, endianess)
        {
            Identifier = identifier;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];
            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return CommitAddress;
        }

        public override uint GetSize()
        {
            return 0;
        }
    }
}
