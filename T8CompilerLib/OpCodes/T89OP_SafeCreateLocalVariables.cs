using System;
using System.Collections.Generic;

//my favorite class ive written in a long time
namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_SafeCreateLocalVariables : T89OpCode
    {
        /* The Idea™ :
         * Need a fast lookup method for both the index of the variable AND inserting the variable
         * Dictionary<uint VarHash, byte StackPosition>
         * Add should be o(1) because we can do Stack[hash] = stack.size;
         * A get simply needs to invert its index by doing Stack.Count - Stack[VarHash] - 1
         * Remove, however, will be o(n) because to remove we need to do value = stack[hash]; stack.remove(hash); foreach(hash in stack.keys) if(stack[hash] > value) stack[hash]--;
         * Lookups should be o(1) because hashmap
         * 
         * All GetLocalvariablewhatever(s) should reference this object directly, then when they need to commit, should query for the location of their local variable.
         * Emit will be o(n) of course.
         */

        private Dictionary<uint, byte> Stack;

        public T89OP_SafeCreateLocalVariables()
        {
            Stack = new Dictionary<uint, byte>();
        }

        protected override ScriptOpCode Code
        {
            get
            {
                return Stack.Count > 0 ? ScriptOpCode.SafeCreateLocalVariables : ScriptOpCode.CheckClearParams;
            }
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            if (Stack.Count < 1)
                return data;

            data[2] = (byte)Stack.Count;

            uint BaseAddress = GetCommitDataAddress() - CommitAddress;

            foreach(var key in Stack.Keys)
            {
                uint index = (uint)(Stack[key]) * 8;
                
                BitConverter.GetBytes(key).CopyTo(data, BaseAddress + index);
            }

            return data;
        }

        public uint GetStackValue(byte index)
        {
            index = (byte)(Stack.Count + ~(int)index);

            foreach (var key in Stack.Keys)
                if (Stack[key] == index)
                    return key;
            return 0u;
        }

        public uint GetListValue(byte index)
        {
            foreach (var key in Stack.Keys)
                if (Stack[key] == index)
                    return key;
            return 0u;
        }

        public override uint GetCommitDataAddress()
        {
            if (Stack.Count < 1)
                return (CommitAddress + T89OP_SIZE);

            return (CommitAddress + T89OP_SIZE + 1).AlignValue(0x4);
        }

        /// <summary>
        /// Add a local variable to the stack and return its reference index
        /// </summary>
        /// <param name="VarHash"></param>
        /// <returns></returns>
        public byte AddLocal(uint VarHash)
        {
            if (Stack.ContainsKey(VarHash))
                return (byte)(Stack.Count - Stack[VarHash] - 1);

            Stack[VarHash] = (byte)Stack.Count;

            return (byte)(Stack.Count - Stack[VarHash] - 1);
        }

        /// <summary>
        /// Try to get a local variable's index
        /// </summary>
        /// <param name="VarHash"></param>
        /// <param name="Index"></param>
        /// <returns></returns>
        public bool TryGetLocal(uint VarHash, out byte Index)
        {
            Index = 0;

            bool result = Stack.ContainsKey(VarHash);

            if (result)
                Index = (byte)(Stack.Count - Stack[VarHash] - 1);

            return result;
        }

        /// <summary>
        /// Remove a local from the stack
        /// </summary>
        /// <param name="VarHash"></param>
        public void RemoveLocal(uint VarHash)
        {
            if (!Stack.TryGetValue(VarHash, out byte value))
                return;

            foreach (uint key in Stack.Keys)
                if (Stack[key] > value)
                    Stack[key]--;

            Stack.Remove(VarHash);
        }

        public override uint GetSize()
        {
            uint address = GetCommitDataAddress();

            for (int i = 0; i < Stack.Count; i++)
            {
                address += 4 + 1; //They emit null terminated dwords... then align to dword boundaries... WHY THE FUCK?!

                if(i + 1 == Stack.Count)
                    address = address.AlignValue(0x2);
                else
                    address = address.AlignValue(0x4);
            }
                
            return address - CommitAddress;
        }
    }
}
