using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_EvalFieldVariable : T7OpCode
    {
        private uint VariableHash;
        public T7OP_EvalFieldVariable(uint variable_hash, uint context, EndianType endianess) : base(endianess)
        {
            if ((context & (uint)ScriptContext.IsRef) > 0)
                Code = ScriptOpCode.EvalFieldVariableRef;
            else
                Code = ScriptOpCode.EvalFieldVariable;

            VariableHash = variable_hash;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            VariableHash.GetBytes(Endianess).CopyTo(data, GetCommitDataAddress() - CommitAddress);

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
