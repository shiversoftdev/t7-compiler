using System;

namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_SetLocal : T89OpCode
    {

        private T89OP_SafeCreateLocalVariables LocalVariables;
        private uint Hash;

        public T89OP_SetLocal(T89OP_SafeCreateLocalVariables cache, uint hash)
        {
            if (!cache.TryGetLocal(hash, out byte index))
                throw new ArgumentException("Tried to access an undefined variable.");
            
            LocalVariables = cache;
            Hash = hash;
            Code = ScriptOpCode.SetLocalVariableCached;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            if (!LocalVariables.TryGetLocal(Hash, out byte Index))
                throw new ArgumentException("A local variable that was referenced inside the export was removed from the variables cache without purging references to it");

            data[T89OP_SIZE] = Index;

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

        public override string ToString()
        {
            return "var_" + Hash.ToString("X");
        }
    }
}
