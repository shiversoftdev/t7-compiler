using System;
using T89CompilerLib.ScriptComponents;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_GetFuncPtr : T89OP_AbstractCall
    {
        internal T89OP_GetFuncPtr(T89Import import, ScriptOpCode code)
        {
            Code = code;
            Import = import;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            uint WriteAddress = (GetCommitDataAddress()).AlignValue(0x8) - CommitAddress;

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
            return (GetCommitDataAddress()).AlignValue(0x8) + 4 + 4 - CommitAddress;
        }
    }
}
