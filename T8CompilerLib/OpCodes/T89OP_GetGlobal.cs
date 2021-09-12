using System;
using T89CompilerLib.ScriptComponents;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_GetGlobal : T89OpCode
    {
        private T89GlobalRef _global;
        private T89GlobalRef Global
        { 
            get
            {
                return _global;
            }
            set
            {
                _global?.References.Remove(this);
                _global = value;
                _global?.References.Add(this);
            }
        }
        public T89OP_GetGlobal(T89GlobalRef global, bool isRef)
        {
            Code = isRef ? ScriptOpCode.GetGlobalObjectRef : ScriptOpCode.GetGlobalObject;
            Global = global;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];
            base.Serialize(EmissionValue).CopyTo(data, 0);
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
