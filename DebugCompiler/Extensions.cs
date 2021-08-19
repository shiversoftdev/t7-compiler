using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using T7MemUtil;

namespace DebugCompiler
{
    internal static class Extensions
    {
        public static IntPtr Open(this Process process, int dwDesiredAccess = T7Memory.PROCESS_ACCESS)
        {
            return T7Memory.OpenProcess(dwDesiredAccess, false, process.Id);
        }

        public static IntPtr Relocate(this Process process, IntPtr Handle)
        {
            return new IntPtr(process.MainModule.BaseAddress.ToInt64() + Handle.ToInt64());
        }

        public static IntPtr Add(this IntPtr pointer, long value)
        {
            return new IntPtr(value + pointer.ToInt64());
        }

        public static IntPtr Relocate(this Process process, uint Handle)
        {
            return process.Relocate(new IntPtr(Handle));
        }

        public static long ReadInt64(this IntPtr ProcessHandle, IntPtr Address)
        {
            byte[] buffer = new byte[sizeof(long)];
            IntPtr numBytes = IntPtr.Zero;
            T7Memory.ReadProcessMemory(ProcessHandle, Address, buffer, (IntPtr)sizeof(long), ref numBytes);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static uint ReadUInt32(this IntPtr ProcessHandle, IntPtr Address)
        {
            byte[] buffer = new byte[sizeof(uint)];
            IntPtr numBytes = IntPtr.Zero;
            T7Memory.ReadProcessMemory(ProcessHandle, Address, buffer, (IntPtr)sizeof(uint), ref numBytes);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static byte[] ReadBytes(this IntPtr ProcessHandle, IntPtr Address, int Count)
        {
            byte[] buffer = new byte[Count];
            IntPtr numBytes = IntPtr.Zero;
            T7Memory.ReadProcessMemory(ProcessHandle, Address, buffer, (IntPtr)Count, ref numBytes);
            return buffer;
        }

        public static IntPtr ReadVoidPtr(this IntPtr ProcessHandle, IntPtr Address)
        {
            return new IntPtr(ProcessHandle.ReadInt64(Address));
        }

        public static T ToStruct<T>(this byte[] data) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            T val = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return val;
        }

        public static T ReadStruct<T>(this IntPtr ProcessHandle, IntPtr Address) where T : struct
        {
            return ProcessHandle.ReadBytes(Address, Marshal.SizeOf(typeof(T))).ToStruct<T>();
        }
    }
}
