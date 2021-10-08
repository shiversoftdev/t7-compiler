using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T7CompilerLib.ScriptComponents;

namespace T7CompilerLib.OpCodes
{
    public class T7OP_LazyGetFunction : T7OpCode
    {
        private uint Namespace;
        private uint Function;

        private T7StringTableEntry __ref__;
        public T7StringTableEntry ReferencedString
        {
            get => __ref__;
            private set
            {
                __ref__?.LazyReferences.Remove(this);
                __ref__ = value;
                __ref__?.LazyReferences.Add(this);
            }
        }

        internal T7OP_LazyGetFunction(uint ns, uint func, T7StringTableEntry refstring) : base(System.IO.EndianType.LittleEndian)
        {
            Code = ScriptOpCode.LazyGetFunction;
            ReferencedString = refstring;
            Namespace = ns;
            Function = func;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            uint WriteAddress = GetCommitDataAddress().AlignValue(0x4) - CommitAddress;

            Namespace.GetBytes(Endianess).CopyTo(data, WriteAddress);
            Function.GetBytes(Endianess).CopyTo(data, WriteAddress + 0x4);
            // script name offset will be fixed up here which is: distance from instruction pointer after passing opcode

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
            return (GetCommitDataAddress()).AlignValue(0x4) + (4 * 3) - CommitAddress;
        }
    }
}
