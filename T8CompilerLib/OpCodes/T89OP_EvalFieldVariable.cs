using System;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_EvalFieldVariable : T89OpCode
    {
        public uint VariableHash { get; private set; }
        public T89OP_EvalFieldVariable(uint variable_hash, uint context)
        {
            if ((context & (uint)ScriptContext.IsRef) > 0)
                Code = ScriptOpCode.EvalFieldVariableRef;
            else
                Code = ScriptOpCode.CastAndEvalFieldVariable;

            VariableHash = variable_hash;
        }

        public void AdjustContext(bool isRef)
        {
            if(isRef)
                Code = ScriptOpCode.EvalFieldVariableRef;
            else
                Code = ScriptOpCode.EvalFieldVariable;
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
