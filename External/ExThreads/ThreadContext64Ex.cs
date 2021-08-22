using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.ExThreads
{
    [Flags]
    internal enum CONTEXT64_FLAGS : uint
    {
        CONTEXT64_AMD64 = 0x100000,
        CONTEXT64_CONTROL = CONTEXT64_AMD64 | 0x01,
        CONTEXT64_INTEGER = CONTEXT64_AMD64 | 0x02,
        CONTEXT64_SEGMENTS = CONTEXT64_AMD64 | 0x04,
        CONTEXT64_FLOATING_POINT = CONTEXT64_AMD64 | 0x08,
        CONTEXT64_DEBUG_REGISTERS = CONTEXT64_AMD64 | 0x10,
        CONTEXT64_FULL = CONTEXT64_CONTROL | CONTEXT64_INTEGER | CONTEXT64_FLOATING_POINT,
        CONTEXT64_ALL = CONTEXT64_CONTROL | CONTEXT64_INTEGER | CONTEXT64_SEGMENTS | CONTEXT64_FLOATING_POINT | CONTEXT64_DEBUG_REGISTERS
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct M128A
    {
        public ulong High;
        public long Low;

        public override string ToString()
        {
            return string.Format("High:{0}, Low:{1}", this.High, this.Low);
        }
    }

    /// <summary>
    /// x64
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct XSAVE_FORMAT64
    {
        public ushort ControlWord;
        public ushort StatusWord;
        public byte TagWord;
        public byte Reserved1;
        public ushort ErrorOpcode;
        public uint ErrorOffset;
        public ushort ErrorSelector;
        public ushort Reserved2;
        public uint DataOffset;
        public ushort DataSelector;
        public ushort Reserved3;
        public uint MxCsr;
        public uint MxCsr_Mask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public M128A[] FloatRegisters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public M128A[] XmmRegisters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public byte[] Reserved4;
    }

    /// <summary>
    /// x64
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal class CONTEXT64
    {
        public ulong P1Home;
        public ulong P2Home;
        public ulong P3Home;
        public ulong P4Home;
        public ulong P5Home;
        public ulong P6Home;

        public CONTEXT64_FLAGS ContextFlags;
        public uint MxCsr;

        public ushort SegCs;
        public ushort SegDs;
        public ushort SegEs;
        public ushort SegFs;
        public ushort SegGs;
        public ushort SegSs;
        public uint EFlags;

        public ulong Dr0;
        public ulong Dr1;
        public ulong Dr2;
        public ulong Dr3;
        public ulong Dr6;
        public ulong Dr7;

        public ulong Rax;
        public ulong Rcx;
        public ulong Rdx;
        public ulong Rbx;
        public ulong Rsp;
        public ulong Rbp;
        public ulong Rsi;
        public ulong Rdi;
        public ulong R8;
        public ulong R9;
        public ulong R10;
        public ulong R11;
        public ulong R12;
        public ulong R13;
        public ulong R14;
        public ulong R15;
        public ulong Rip;

        public XSAVE_FORMAT64 DUMMYUNIONNAME;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
        public M128A[] VectorRegister;
        public ulong VectorControl;

        public ulong DebugControl;
        public ulong LastBranchToRip;
        public ulong LastBranchFromRip;
        public ulong LastExceptionToRip;
        public ulong LastExceptionFromRip;
    }

    public class ThreadContext64Ex : ThreadContextEx
    {

        CONTEXT64 ctx = new CONTEXT64();

        public override PointerEx InstructionPointer
        {
            get => ctx.Rip; set => ctx.Rip = value;
        }

        protected override object ContextStruct { get => ctx; set => ctx = (CONTEXT64)value; }

        public ThreadContext64Ex(ThreadContextExFlags contextFlags)
        {
            SetFlags(contextFlags);
        }

        public void SetFlags(ThreadContextExFlags contextFlags)
        {
            switch (contextFlags)
            {
                case ThreadContextExFlags.All:
                    ctx.ContextFlags = CONTEXT64_FLAGS.CONTEXT64_ALL;
                    break;
                case ThreadContextExFlags.Debug:
                    ctx.ContextFlags = CONTEXT64_FLAGS.CONTEXT64_DEBUG_REGISTERS;
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

        public void SetDebug(PointerEx dr0, PointerEx dr1, PointerEx dr2)
        {
            ctx.ContextFlags = CONTEXT64_FLAGS.CONTEXT64_DEBUG_REGISTERS;
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
