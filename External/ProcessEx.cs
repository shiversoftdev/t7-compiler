using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.PEStructures;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static System.ModuleLoadOptions;
using static System.ModuleLoadType;

namespace System
{
    public class ProcessEx
    {
        #region const
        public const int PROCESS_ACCESS = PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int PAGE_READWRITE = 0x04;
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int MEM_DECOMMIT = 0x4000;
        public const int MEM_FREE = 0x10000;
        public const int MEM_COMMIT = 0x00001000;
        public const int MEM_RESERVE = 0x00002000;
        public const int MEM_PRIVATE = 0x20000;
        public const int MEM_IMAGE = 0x1000000;
        public const uint PAGE_GUARD = 0x100;
        public const uint PAGE_NOACCESS = 0x01;
        public const uint PAGE_READONLY = 0x02;
        public const uint PAGE_WRITECOPY = 0x08;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;
        public const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        public const uint PAGE_EXECUTE = 0x10;
        public const uint PAGE_EXECUTE_READ = 0x20;
        #endregion

        #region typedef
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            internal ushort wProcessorArchitecture;
            internal ushort wReserved;
            internal uint dwPageSize;
            internal PointerEx lpMinimumApplicationAddress;
            internal PointerEx lpMaximumApplicationAddress;
            internal PointerEx dwActiveProcessorMask;
            internal uint dwNumberOfProcessors;
            internal uint dwProcessorType;
            internal uint dwAllocationGranularity;
            internal ushort wProcessorLevel;
            internal ushort wProcessorRevision;
        }

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        public struct MEMORY_BASIC_INFORMATION
        {
            public PointerEx BaseAddress;
            public PointerEx AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public PointerEx RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }
        #endregion

        #region dllimport
        [DllImport("kernel32.dll")]
        public static extern PointerEx OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(PointerEx hProcess, PointerEx lpBaseAddress, [Out] byte[] lpBuffer, PointerEx dwSize, ref PointerEx lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(PointerEx hProcess, out uint ExitCode);

        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(PointerEx handle);

        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern PointerEx VirtualQueryEx(PointerEx hProcess, PointerEx lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(PointerEx hProcess, PointerEx lpBaseAddress, byte[] lpBuffer, int dwSize, ref PointerEx lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualAlloc2(PointerEx hProcess, PointerEx lpBaseAddress, int RegionSize, ulong AllocType, ulong PageProtection);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern PointerEx VirtualAllocEx(PointerEx hProcess, PointerEx lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(PointerEx hProcess, PointerEx lpAddress, uint dwSize, int dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(PointerEx hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool IsWow64Process(PointerEx processHandle, out bool isWow64Process);
        #endregion

        #region methods
        public ProcessEx(Process p, bool openHandle = false) 
        {
            if (p == null) throw new ArgumentException("Target process cannot be null");

            BaseProcess = p;
            p.EnableRaisingEvents = true;
            p.Exited += P_Exited;
            if(openHandle) OpenHandle();
        }

        private void P_Exited(object sender, EventArgs e) 
        {
            Handle = IntPtr.Zero;
        }

        public PointerEx OpenHandle(int dwDesiredAccess = PROCESS_ACCESS, bool newOnly = false) 
        {
            if (BaseProcess.HasExited) return IntPtr.Zero;
            if (Handle.IntPtr == IntPtr.Zero || newOnly) Handle = OpenProcess(dwDesiredAccess, false, BaseProcess.Id);
            return Handle;
        }

        public void CloseHandle()
        {
            if (!Handle) return;
            CloseHandle(Handle);
            Handle = 0;
        }

        public static ProcessEx FindProc(string name, bool OpenHandle = false)
        {
            var list = Process.GetProcessesByName(name);
            if (list.Length < 1) return null;
            return new ProcessEx(list[0], OpenHandle);
        }

        public T GetValue<T>(PointerEx absoluteAddress) where T : new()
        {
            if (!Handle) throw new InvalidOperationException("Tried to read from a memory region when a handle to the desired process doesn't exist");
            PointerEx size = Marshal.SizeOf(new T());
            byte[] data = GetBytes(absoluteAddress, size);
            if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(PointerEx))
            {
                IntPtr val = IntPtr.Size == sizeof(int) ? (IntPtr)BitConverter.ToInt32(data, 0) : (IntPtr)BitConverter.ToInt64(data, 0);
                if (typeof(T) == typeof(IntPtr)) return (dynamic)val;
                return (dynamic)new PointerEx(val);
            }
            if (typeof(T) == typeof(float)) return (dynamic)BitConverter.ToSingle(data, 0);
            if (typeof(T) == typeof(long)) return (dynamic)BitConverter.ToInt64(data, 0);
            if (typeof(T) == typeof(ulong)) return (dynamic)BitConverter.ToUInt64(data, 0);
            if (typeof(T) == typeof(int)) return (dynamic)BitConverter.ToInt32(data, 0);
            if (typeof(T) == typeof(uint)) return (dynamic)BitConverter.ToUInt32(data, 0);
            if (typeof(T) == typeof(short)) return (dynamic)BitConverter.ToInt16(data, 0);
            if (typeof(T) == typeof(ushort)) return (dynamic)BitConverter.ToUInt16(data, 0);
            if (typeof(T) == typeof(byte)) return (dynamic)data[0];
            if (typeof(T) == typeof(sbyte)) return (dynamic)data[0];
            throw new InvalidCastException($"Type {typeof(T)} is not a valid value type");
        }

        public void SetValue<T>(PointerEx absoluteAddress, T value) where T : new()
        {
            if (!Handle) throw new InvalidOperationException("Tried to write to a memory region when a handle to the desired process doesn't exist");
            byte[] data = Array.Empty<byte>();
            if (value is IntPtr ip) data = IntPtr.Size == sizeof(int) ? BitConverter.GetBytes(ip.ToInt32()) : BitConverter.GetBytes(ip.ToInt64());
            else if (value is PointerEx ipx) data = IntPtr.Size == sizeof(int) ? BitConverter.GetBytes(ipx.IntPtr.ToInt32()) : BitConverter.GetBytes(ipx.IntPtr.ToInt64());
            else if (value is float f) data = BitConverter.GetBytes(f);
            else if (value is long l) data = BitConverter.GetBytes(l);
            else if (value is ulong ul) data = BitConverter.GetBytes(ul);
            else if (value is int i) data = BitConverter.GetBytes(i);
            else if (value is uint ui) data = BitConverter.GetBytes(ui);
            else if (value is short s) data = BitConverter.GetBytes(s);
            else if (value is ushort u) data = BitConverter.GetBytes(u);
            else if (value is byte b) data = new byte[] { b };
            else if (value is sbyte sb) data = new byte[] { (byte)sb };
            else throw new InvalidCastException($"Cannot use type {typeof(T)} as a value type");
            SetBytes(absoluteAddress, data);
        }

        public byte[] GetBytes(PointerEx absoluteAddress, PointerEx NumBytes) 
        {
            if (!Handle) throw new InvalidOperationException("Tried to read from a memory region when a handle to the desired process doesn't exist");
            byte[] data = new byte[NumBytes];
            PointerEx bytesRead = IntPtr.Zero;
            ReadProcessMemory(Handle, absoluteAddress, data, NumBytes, ref bytesRead);
            if (bytesRead != NumBytes) throw new InvalidOperationException($"Failed to read data of size {NumBytes} from address 0x{absoluteAddress}");
            return data;
        }

        public void SetBytes(PointerEx absoluteAddress, byte[] data) 
        {
            if (!Handle) throw new InvalidOperationException("Tried to write to a memory region when a handle to the desired process doesn't exist");
            PointerEx bytesWritten = IntPtr.Zero;
            WriteProcessMemory(Handle, absoluteAddress, data, data.Length, ref bytesWritten);
            if (bytesWritten != data.Length) throw new InvalidOperationException($"Failed to write {data.Length} bytes to region 0x{absoluteAddress}");
        }

        public T GetStruct<T>(PointerEx absoluteAddress) where T : struct
        {
            return GetBytes(absoluteAddress, Marshal.SizeOf(typeof(T))).ToStruct<T>();
        }

        public void SetStruct<T>(PointerEx absoluteAddress, T s) where T : struct
        {
            SetBytes(absoluteAddress, s.ToByteArray());
        }

        public T[] GetArray<T>(PointerEx absoluteAddress, PointerEx numItems) where T : struct
        {
            T[] arr = new T[numItems];
            for(int i = 0; i < numItems; i++) arr[i] = GetStruct<T>(absoluteAddress + (i * Marshal.SizeOf(typeof(T))));
            return arr;
        }

        public void SetArray<T>(PointerEx absoluteAddress, T[] array) where T : struct
        {
            for (int i = 0; i < array.Length; i++) SetStruct(absoluteAddress + (i * Marshal.SizeOf(typeof(T))), array[i]);
        }

        public string GetString(PointerEx absoluteAddress, int MaxLength = 1023, int buffSize = 256) 
        {
            byte[] buffer;
            byte[] rawString = new byte[MaxLength + 1];
            int bytesRead = 0;
            while(bytesRead < MaxLength)
            {
                buffer = GetBytes(absoluteAddress + bytesRead, buffSize);
                for(int i = 0; i < buffer.Length && i + bytesRead < MaxLength; i++)
                {
                    if (buffer[i] == 0) return rawString.String();
                    rawString[bytesRead + i] = buffer[i];
                }
                bytesRead += buffSize;
            }
            return rawString.String();
        }

        public void SetString(PointerEx absoluteAddress, string Value) 
        {
            SetArray(absoluteAddress, Value.Bytes());
        }

        public Task<IEnumerable<PointerEx>> FindPattern(string query, PointerEx start, PointerEx end, MemorySearchFlags flags)
        {
            return new MemorySearcher(this).Search(query, start, end, flags);
        }

        public PointerEx LoadModule(Memory<byte> moduleData, ModuleLoadOptions loadOptions = MLO_None)
        {
            if (BaseProcess.HasExited) throw new InvalidOperationException("Cannot inject a dll to a process which has exited");
            if (moduleData.IsEmpty) throw new ArgumentException("Cannot inject an empty dll");
            if (!Environment.Is64BitProcess && (GetArchitecture() == Architecture.X64)) throw new InvalidOperationException("Cannot map to target; a 32-bit injector cannot inject to a 64 bit process.");
            var _pe = new PEImage(moduleData);

            return IntPtr.Zero; // failed to load
        }

        internal Architecture GetArchitecture()
        {
            if (!Environment.Is64BitOperatingSystem || IsWow64Process()) return Architecture.X86;
            return Architecture.X64;
        }

        internal bool IsWow64Process()
        {
            if(!IsWow64Process(BaseProcess.Handle, out bool result)) throw new ComponentModel.Win32Exception();
            return result;
        }
        #endregion

        #region overrides
        public static implicit operator ProcessEx(Process p)
        {
            return new ProcessEx(p);
        }

        public static implicit operator Process(ProcessEx px)
        {
            return px.BaseProcess;
        }

        public static implicit operator ProcessEx(string name)
        {
            return FindProc(name);
        }

        public static implicit operator bool(ProcessEx px)
        {
            return px?.Handle ?? false;
        }

        public PointerEx this[PointerEx offset]
        {
            get
            {
                return BaseAddress + offset;
            }
        }

        public ProcessModuleEx this[string name]
        {
            get
            {
                foreach(var m in Modules)
                {
                    if (m.BaseModule.ModuleName.Equals(name, StringComparison.InvariantCultureIgnoreCase)) return m;
                }
                return null;
            }
        }
        #endregion

        #region Members
        public Process BaseProcess { get; private set; }
        public PointerEx BaseAddress
        { 
            get
            {
                return BaseProcess.MainModule.BaseAddress;
            }
        }

        private PointerEx __handle = IntPtr.Zero;
        public PointerEx Handle
        {
            get
            {
                if (BaseProcess.HasExited) return IntPtr.Zero;
                return __handle;
            }
            private set
            {
                __handle = value;
            }
        }

        public IEnumerable<ProcessModuleEx> Modules
        {
            get
            {
                foreach (ProcessModule p in BaseProcess.Modules) yield return p;
            }
        }
        #endregion
    }

    #region public Typedef
    #region enum
    public enum ModuleLoadType
    { 
        MLT_ManualMapped
    }

    public enum ModuleLoadOptions
    {
        MLO_None = 0
    }
    #endregion
    #region struct
    [StructLayout(LayoutKind.Explicit, Size = 1152)]
    public readonly struct Peb32
    {
        [FieldOffset(0xC)]
        public readonly int Ldr;

        [FieldOffset(0x38)]
        public readonly int APISetMap;
    }

    [StructLayout(LayoutKind.Explicit, Size = 1992)]
    public readonly struct Peb64
    {
        [FieldOffset(0x18)]
        public readonly long Ldr;

        [FieldOffset(0x68)]
        public readonly long APISetMap;
    }
    #endregion
    #endregion
}
