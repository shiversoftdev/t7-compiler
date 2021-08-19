using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_CallPtr : T7OpCode
    {
        private byte NumParams;
        public T7OP_CallPtr(uint context, byte numparams, EndianType endianess) : base (endianess)
        {
            NumParams = numparams;

            if(context.HasContext(ScriptContext.HasCaller))
            {
                Code = context.HasContext(ScriptContext.Threaded) ? ScriptOpCode.ScriptMethodThreadCallPointer : ScriptOpCode.ScriptMethodCallPointer;
            }
            else
            {
                Code = context.HasContext(ScriptContext.Threaded) ? ScriptOpCode.ScriptThreadCallPointer : ScriptOpCode.ScriptFunctionCallPointer;
            }
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            data[T7OP_SIZE] = NumParams;

            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return CommitAddress + T7OP_SIZE;
        }

        public override uint GetSize()
        {
            if(Endianess == EndianType.LittleEndian)
                return T7OP_SIZE + 1 + 1;
            return T7OP_SIZE + 1;
        }
    }
}
