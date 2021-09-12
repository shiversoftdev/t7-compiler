using System;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_GetHash : T89OpCode
    {
        ulong Hash;
        public T89OP_GetHash(ulong hash) : base(ScriptOpCode.GetHash)
        {
            Hash = hash;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            BitConverter.GetBytes(Hash).CopyTo(data, GetCommitDataAddress() - CommitAddress);

            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return (CommitAddress + T89OP_SIZE).AlignValue(sizeof(ulong));
        }

        public override uint GetSize()
        {
            return 8 + GetCommitDataAddress() - CommitAddress;
        }
    }
}
