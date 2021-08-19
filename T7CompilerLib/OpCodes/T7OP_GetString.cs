using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T7CompilerLib.ScriptComponents;

//Class should be finished.
namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_GetString : T7OpCode
    {
        private T7StringTableEntry __ref__;
        public T7StringTableEntry ReferencedString
        {
            get => __ref__;
            private set
            {
                __ref__?.References.Remove(this);
                __ref__ = value;
                __ref__?.References.Add(this);
            }
        }

        public T7OP_GetString(ScriptOpCode op_info, T7StringTableEntry refstring, EndianType endianess) : base(op_info, endianess) 
        {
            ReferencedString = refstring;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            //We dont need to pass a real pointer here. Game overwrites it on load anyways
            if(Endianess == EndianType.LittleEndian)
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }.CopyTo(data, GetCommitDataAddress() - CommitAddress);
            else
                new byte[] { 0xFF, 0xFF }.CopyTo(data, GetCommitDataAddress() - CommitAddress);

            return data;
        }

        public override uint GetCommitDataAddress()
        {
            if (Endianess == EndianType.LittleEndian)
                return (CommitAddress + T7OP_SIZE).AlignValue(0x4);

            return (CommitAddress + T7OP_SIZE).AlignValue(0x2);
        }

        public override uint GetSize()
        {
            if (Endianess == EndianType.LittleEndian)
                return 4 + GetCommitDataAddress() - CommitAddress;
            return 2 + GetCommitDataAddress() - CommitAddress;
        }
    }
}
