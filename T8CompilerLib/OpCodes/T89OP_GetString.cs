using T89CompilerLib.ScriptComponents;

//Class should be finished.
namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_GetString : T89OpCode
    {
        private T89StringTableEntry __ref__;
        public T89StringTableEntry ReferencedString
        {
            get => __ref__;
            private set
            {
                __ref__?.References.Remove(this);
                __ref__ = value;
                __ref__?.References.Add(this);
            }
        }

        public T89OP_GetString(ScriptOpCode op_info, T89StringTableEntry refstring) : base(op_info) 
        {
            ReferencedString = refstring;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            //We dont need to pass a real pointer here. Game overwrites it on load anyways
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }.CopyTo(data, GetCommitDataAddress() - CommitAddress);

            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return (CommitAddress + T89OP_SIZE).AlignValue(0x4);
        }

        public override uint GetSize()
        {
            return 4 + GetCommitDataAddress() - CommitAddress;
        }
    }
}
