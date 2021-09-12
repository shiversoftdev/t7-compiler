namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_CallPtr : T89OpCode
    {
        private byte NumParams;
        public T89OP_CallPtr(uint context, byte numparams)
        {
            NumParams = numparams;

            if(context.HasContext(ScriptContext.HasCaller))
            {
                Code = context.HasContext(ScriptContext.Threaded) ? ScriptOpCode.ScriptMethodThreadCallPointer : ScriptOpCode.ScriptMethodCallPointer;
            }
            else
            {
                Code = context.HasContext(ScriptContext.Threaded) ? ScriptOpCode.ScriptThreadCallPointer : ScriptOpCode.ScriptFunctionCallPointer;
            }
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            data[2] = NumParams;

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
