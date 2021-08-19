using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T7CompilerLib.ScriptComponents;

namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_Call : T7OP_AbstractCall
    {
        internal T7OP_Call(T7Import import, uint context, EndianType endianess) : base(endianess)
        {
            if (context.HasContext(ScriptContext.HasCaller))
            {
                Code = context.HasContext(ScriptContext.Threaded) ? ScriptOpCode.ScriptMethodThreadCall : ScriptOpCode.ScriptMethodCall;
            }
            else
            {
                Code = context.HasContext(ScriptContext.Threaded) ? ScriptOpCode.ScriptThreadCall : ScriptOpCode.ScriptFunctionCall;
            }
            Import = import;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            data[T7OP_SIZE] = Import.NumParams;

            if(Endianess == EndianType.LittleEndian)
                data[3] = Import.Flags;

            uint WriteAddress = (GetCommitDataAddress() + T7OP_SIZE).AlignValue(T7OP_SIZE * 4) - CommitAddress;

            Import.Function.GetBytes(Endianess).CopyTo(data, WriteAddress);
            
            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return CommitAddress + T7OP_SIZE;
        }

        //OP_CODE 0x2
        //NUMPARAMS 0x1
        //padding 0x1
        //QWORD ALIGN
        //Function
        //0 (x4)
        public override uint GetSize()
        {
            return (GetCommitDataAddress() + T7OP_SIZE).AlignValue(T7OP_SIZE * 4) + (T7OP_SIZE * 4) - CommitAddress;
        }
    }
}
