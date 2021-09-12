using System;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_Jump : T89OpCode
    {
        /// <summary>
        /// The opcode this jump should jump past
        /// </summary>
        public T89OpCode After;

        /// <summary>
        /// Should this jump use the loop head as a ref, or the loop end as a ref.
        /// </summary>
        public bool RefHead { get; internal set; }

        internal T89OP_Jump(ScriptOpCode code)
        {
            switch(code)
            {
                case ScriptOpCode.Jump:
                case ScriptOpCode.JumpOnFalse:
                case ScriptOpCode.JumpOnFalseExpr:
                case ScriptOpCode.JumpOnTrue:
                case ScriptOpCode.JumpOnTrueExpr:
                    break;

                default:
                    throw new ArgumentException("Cannot initialize a jump object with a non-jump related operation");
            }
            Code = code;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            //This is just for identification of mis-written jumps in the output binary (if there are any)
            data[2] = 0xFF;
            data[3] = 0xFF;

            return data;
        }

        internal void CommitJump(ref byte[] data)
        {
            if (After == null)
                throw new NotImplementedException("A jump was recorded, but the opcode to jump to was never set...");

            uint JumpTo = After.CommitAddress + After.GetSize();
            uint JumpFrom = CommitAddress + GetSize();

            BitConverter.GetBytes((short)(JumpTo - JumpFrom)).CopyTo(data, GetCommitDataAddress());
        }

        public override uint GetCommitDataAddress()
        {
            return CommitAddress + T89OP_SIZE;
        }

        public override uint GetSize()
        {
            return T89OP_SIZE + 2;
        }
    }
}
