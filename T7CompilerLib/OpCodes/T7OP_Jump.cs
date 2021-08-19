using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_Jump : T7OpCode
    {
        /// <summary>
        /// The opcode this jump should jump past
        /// </summary>
        public T7OpCode After;

        /// <summary>
        /// Should this jump use the loop head as a ref, or the loop end as a ref.
        /// </summary>
        public bool RefHead { get; internal set; }

        internal T7OP_Jump(ScriptOpCode code, EndianType endianess) : base(endianess)
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
            data[data.Length - 2] = 0xFF;
            data[data.Length - 1] = 0xFF;

            return data;
        }

        internal void CommitJump(ref byte[] data)
        {
            if (After == null)
                throw new NotImplementedException("A jump was recorded, but the opcode to jump to was never set...");

            uint JumpTo = After.CommitAddress + After.GetSize();
            uint JumpFrom = CommitAddress + GetSize();

            ((short)(JumpTo - JumpFrom)).GetBytes(Endianess).CopyTo(data, GetCommitDataAddress());
        }

        public override uint GetCommitDataAddress()
        {
            return (CommitAddress + T7OP_SIZE).AlignValue(0x2);
        }

        public override uint GetSize()
        {
            return GetCommitDataAddress() + 2 - CommitAddress;
        }
    }
}
