using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.ExThreads
{
    [Flags]
    internal enum CONTEXT_FLAGS : uint
    {
        CONTEXT_i386 = 0x10000,
        CONTEXT_i486 = 0x10000,   //  same as i386
        CONTEXT_CONTROL = CONTEXT_i386 | 0x01, // SS:SP, CS:IP, FLAGS, BP
        CONTEXT_INTEGER = CONTEXT_i386 | 0x02, // AX, BX, CX, DX, SI, DI
        CONTEXT_SEGMENTS = CONTEXT_i386 | 0x04, // DS, ES, FS, GS
        CONTEXT_FLOATING_POINT = CONTEXT_i386 | 0x08, // 387 state
        CONTEXT_DEBUG_REGISTERS = CONTEXT_i386 | 0x10, // DB 0-3,6,7
        CONTEXT_EXTENDED_REGISTERS = CONTEXT_i386 | 0x20, // cpu specific extensions
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FLOATING_SAVE_AREA
    {
        public uint ControlWord;
        public uint StatusWord;
        public uint TagWord;
        public uint ErrorOffset;
        public uint ErrorSelector;
        public uint DataOffset;
        public uint DataSelector;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] RegisterArea;
        public uint Cr0NpxState;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class CONTEXT
    {
        public CONTEXT_FLAGS ContextFlags; //set this to an appropriate value
                                           // Retrieved by CONTEXT_DEBUG_REGISTERS
        public uint Dr0;
        public uint Dr1;
        public uint Dr2;
        public uint Dr3;
        public uint Dr6;
        public uint Dr7;
        // Retrieved by CONTEXT_FLOATING_POINT
        public FLOATING_SAVE_AREA FloatSave;
        // Retrieved by CONTEXT_SEGMENTS
        public uint SegGs;
        public uint SegFs;
        public uint SegEs;
        public uint SegDs;
        // Retrieved by CONTEXT_INTEGER
        public uint Edi;
        public uint Esi;
        public uint Ebx;
        public uint Edx;
        public uint Ecx;
        public uint Eax;
        // Retrieved by CONTEXT_CONTROL
        public uint Ebp;
        public uint Eip;
        public uint SegCs;
        public uint EFlags;
        public uint Esp;
        public uint SegSs;
        // Retrieved by CONTEXT_EXTENDED_REGISTERS
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] ExtendedRegisters;
    }

    public class ThreadContext32Ex : ThreadContextEx
    {
        private CONTEXT ctx = new CONTEXT();

        public override PointerEx InstructionPointer
        {
            get => ctx.Eip; set => ctx.Eip = value;
        }

        protected override object ContextStruct { get => ctx; set => ctx = (CONTEXT)value; }

        public ThreadContext32Ex(ThreadContextExFlags contextFlags)
        {
            SetFlags(contextFlags);
        }

        public void SetFlags(ThreadContextExFlags contextExFlags)
        {
            switch (contextExFlags)
            {
                case ThreadContextExFlags.All:
                    ctx.ContextFlags = CONTEXT_FLAGS.CONTEXT_ALL;
                    break;
                case ThreadContextExFlags.Debug:
                    ctx.ContextFlags = CONTEXT_FLAGS.CONTEXT_DEBUG_REGISTERS;
                    break;
            }
        }

        protected override bool SetContext(PointerEx thread, PointerEx context)
        {
            return NativeStealth.SetThreadContext(thread, context);
        }

        protected override bool GetContext(PointerEx thread, PointerEx context)
        {
            return NativeStealth.GetThreadContext(thread, context);
        }

        /// <summary>
        /// Switch threadcontext flags to debug, and apply dr configurations
        /// </summary>
        /// <param name="dr0"></param>
        /// <param name="dr1"></param>
        /// <param name="dr2"></param>
        public void SetDebug(PointerEx dr0, PointerEx dr1, PointerEx dr2)
        {
            ctx.ContextFlags = CONTEXT_FLAGS.CONTEXT_DEBUG_REGISTERS;
            ctx.Dr7 = 0;
            if (dr0)
            {
                ctx.Dr7 |= 1;
            }
            if (dr1)
            {
                ctx.Dr7 |= 2;
            }
            if (dr2)
            {
                ctx.Dr7 |= 4;
            }
            ctx.Dr0 = dr0;
            ctx.Dr1 = dr1;
            ctx.Dr2 = dr2;
        }
    }
}
