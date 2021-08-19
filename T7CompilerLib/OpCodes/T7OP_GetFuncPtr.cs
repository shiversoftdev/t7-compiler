using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T7CompilerLib.ScriptComponents;

namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_GetFuncPtr : T7OP_AbstractCall
    {
        internal T7OP_GetFuncPtr(T7Import import, EndianType endianess): base(endianess)
        {
            Code = ScriptOpCode.GetFunction;
            Import = import;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            uint WriteAddress = 0u;

            if (Endianess == EndianType.LittleEndian)
                WriteAddress = (GetCommitDataAddress()).AlignValue(0x8) - CommitAddress;
            else
                WriteAddress = (GetCommitDataAddress()).AlignValue(0x4) - CommitAddress;

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
            if (Endianess == EndianType.LittleEndian)
                return (GetCommitDataAddress()).AlignValue(0x8) + 4 + 4 - CommitAddress;

            return (GetCommitDataAddress()).AlignValue(0x4) + 4 - CommitAddress;
        }
    }
}
