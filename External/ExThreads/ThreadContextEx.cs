using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

//https://github.com/CCob/SharpBlock/blob/c7f6fcb9ca9ffa80fe8862b165196098d9afbe20/Context.cs#L32
namespace System.ExThreads
{
    public enum ThreadContextExFlags
    {
        All,
        Debug
    }

    public enum ThreadAccess : int
    {
        TERMINATE = (0x0001),
        SUSPEND_RESUME = (0x0002),
        GET_CONTEXT = (0x0008),
        SET_CONTEXT = (0x0010),
        SET_INFORMATION = (0x0020),
        QUERY_INFORMATION = (0x0040),
        SET_THREAD_TOKEN = (0x0080),
        IMPERSONATE = (0x0100),
        DIRECT_IMPERSONATION = (0x0200),
        THREAD_HIJACK = SUSPEND_RESUME | GET_CONTEXT | SET_CONTEXT,
        THREAD_ALL = TERMINATE | SUSPEND_RESUME | GET_CONTEXT | SET_CONTEXT | SET_INFORMATION | QUERY_INFORMATION | SET_THREAD_TOKEN | IMPERSONATE | DIRECT_IMPERSONATION
    }

    public abstract class ThreadContextEx : IDisposable
    {

        private PointerEx hInternalMemory;
        private PointerEx hAlignedMemory;

        public ThreadContextEx()
        {
            //Get/SetThreadContext needs to be 16 byte aligned memory offset on x64
            hInternalMemory = Marshal.AllocHGlobal(Marshal.SizeOf(ContextStruct) + 1024);
            hAlignedMemory = (long)hInternalMemory & ~0xF;
        }

        public void Dispose()
        {
            if (hInternalMemory)
            {
                Marshal.FreeHGlobal(hInternalMemory);
            }
        }

        public bool GetContext(PointerEx thread)
        {
            Marshal.StructureToPtr(ContextStruct, hAlignedMemory, false);
            bool result = GetContext(thread, hAlignedMemory);
            ContextStruct = Marshal.PtrToStructure(hAlignedMemory, ContextStruct.GetType());
            return result;
        }

        public bool SetContext(PointerEx thread)
        {
            Marshal.StructureToPtr(ContextStruct, hAlignedMemory, false);
            return SetContext(thread, hAlignedMemory);
        }

        public ulong SetBits(ulong dw, int lowBit, int bits, ulong newValue)
        {
            ulong mask = (1UL << bits) - 1UL;
            dw = (dw & ~(mask << lowBit)) | (newValue << lowBit);
            return dw;
        }

        protected abstract object ContextStruct { get; set; }

        protected abstract bool SetContext(PointerEx thread, PointerEx context);

        protected abstract bool GetContext(PointerEx thread, PointerEx context);

        public abstract PointerEx InstructionPointer { get; set; }
    }
}
