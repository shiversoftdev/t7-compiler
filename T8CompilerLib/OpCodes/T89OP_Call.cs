using System;
using T89CompilerLib.ScriptComponents;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_Call : T89OP_AbstractCall
    {
        internal T89OP_Call(T89Import import, uint context)
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

            data[2] = Import.NumParams;
            data[3] = Import.Flags;

            uint WriteAddress = (GetCommitDataAddress() + 2).AlignValue(0x8) - CommitAddress;

            BitConverter.GetBytes(Import.Function).CopyTo(data, WriteAddress);
            //BitConverter.GetBytes(Import.Namespace).CopyTo(data, WriteAddress + 4);
            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return CommitAddress + T89OP_SIZE;
        }

        //OP_CODE 0x2
        //NUMPARAMS 0x1
        //padding 0x1
        //QWORD ALIGN
        //Function
        //0 (x4)
        public override uint GetSize()
        {
            return (GetCommitDataAddress() + 1 + 1).AlignValue(0x8) + 4 + 4 - CommitAddress;
        }
    }
}
