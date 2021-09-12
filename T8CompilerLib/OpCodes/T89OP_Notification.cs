using System;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_Notification : T89OpCode
    {
        private byte NumParams;
        public T89OP_Notification(ScriptOpCode Op, byte numParams)
        {
            Code = Op;
            NumParams = numParams;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];
            base.Serialize(EmissionValue).CopyTo(data, 0);
            data[T89OP_SIZE] = NumParams;
            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return CommitAddress + T89OP_SIZE;
        }

        public override uint GetSize()
        {
            return T89OP_SIZE + 1 + 1;
        }
    }
}
