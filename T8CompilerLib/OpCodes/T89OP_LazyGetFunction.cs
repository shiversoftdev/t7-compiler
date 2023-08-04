using System;

namespace T89CompilerLib.OpCodes
{
    public class T89OP_LazyGetFunction : T89OpCode
    {
        private uint Namespace;
        private uint Function;
        private ulong Script;

        internal T89OP_LazyGetFunction(uint ns, uint func, ulong script) : base(ScriptOpCode.LazyGetFunction)
        {
            Script = script;
            Namespace = ns;
            Function = func;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            uint WriteAddress = GetCommitDataAddress().AlignValue(0x4) - CommitAddress;

            BitConverter.GetBytes(Namespace).CopyTo(data, WriteAddress);
            BitConverter.GetBytes(Function).CopyTo(data, WriteAddress + 4);
            BitConverter.GetBytes(Script).CopyTo(data, WriteAddress + 8);

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
        //Function (2*4)
        //Script (8)
        //0 (x4)
        public override uint GetSize()
        {
            return (GetCommitDataAddress()).AlignValue(0x4) + (4 * 4) - CommitAddress;
        }
    }
}
