using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_GetHash : T7OpCode
    {
        uint Hash;
        public T7OP_GetHash(uint hash, EndianType endianess) : base(ScriptOpCode.GetHash, endianess)
        {
            Hash = hash;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            Hash.GetBytes(Endianess).CopyTo(data, GetCommitDataAddress() - CommitAddress);

            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return (CommitAddress + T7OP_SIZE).AlignValue(0x4);
        }

        public override uint GetSize()
        {
            return 4 + GetCommitDataAddress() - CommitAddress;
        }
    }
}
