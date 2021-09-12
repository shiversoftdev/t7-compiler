using System;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_SetVariableFieldRef : T89OpCode
    {
        private uint VariableHash;
        public T89OP_SetVariableFieldRef(uint variable_hash)
        {
            Code = ScriptOpCode.SetVariableFieldRef;
            VariableHash = variable_hash;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];
            base.Serialize(EmissionValue).CopyTo(data, 0);
            BitConverter.GetBytes(VariableHash).CopyTo(data, GetCommitDataAddress() - CommitAddress);
            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return (CommitAddress + T89OP_SIZE).AlignValue(0x4);
        }

        public override uint GetSize()
        {
            return 4 + GetCommitDataAddress() - CommitAddress;
        }
    }
}
