using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_GetLocal : T7OpCode
    {

        private T7OP_SafeCreateLocalVariables LocalVariables;
        private uint Hash;

        public T7OP_GetLocal(T7OP_SafeCreateLocalVariables cache, uint hash, ScriptOpCode Op, EndianType endianess) : base(endianess)
        {
            if (!cache.TryGetLocal(hash, out byte index))
                throw new ArgumentException("Tried to access an undefined variable.");

            LocalVariables = cache;
            Hash = hash;
            Code = Op;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            if (!LocalVariables.TryGetLocal(Hash, out byte Index))
                throw new ArgumentException("A local variable that was referenced inside the export was removed from the variables cache without purging references to it");

            data[GetCommitDataAddress() - CommitAddress] = Index;

            return data;
        }

        public override uint GetCommitDataAddress()
        {
            return CommitAddress + T7OP_SIZE;
        }

        public override uint GetSize()
        {
            return T7OP_SIZE * 2;
        }
    }
}
