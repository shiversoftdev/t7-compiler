using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.PEStructures;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.EnvironmentEx;
using static System.ModuleLoadOptions;
using static System.ModuleLoadType;
using static System.ExXMMReturnType;
using System.Threading;
using System.Runtime.CompilerServices;
using System.ExThreads;
using System.Reflection.PortableExecutable;

namespace System
{
    public partial class ProcessEx
    {
        #region const
        public const int PROCESS_CREATE_THREAD = 0x02;
        public const int PROCESS_ACCESS = PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_CREATE_THREAD;
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

        #region pinvoke
        [DllImport("kernel32.dll")]
        public static extern PointerEx OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(PointerEx hProcess, out uint ExitCode);

        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(PointerEx handle);

        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern PointerEx VirtualQueryEx(PointerEx hProcess, PointerEx lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(PointerEx hProcess, PointerEx lpAddress, uint dwSize, int dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(PointerEx hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool IsWow64Process(PointerEx processHandle, out bool isWow64Process);
        #endregion

        #region methods
        private Timer ProcInfoTimer;
        private EventHandler ProcInfoUpdate;
        public ProcessEx(Process p, bool openHandle = false) 
        {
            if (p == null) throw new ArgumentException("Target process cannot be null");

            BaseProcess = p;
            p.EnableRaisingEvents = true;
            p.Exited += P_Exited;
            if(openHandle) OpenHandle();
            ProcInfoTimer = new Timer(PInfoTick, null, 0, 1000);
            ProcInfoUpdate += PInfoUpdate;
        }

        private void PInfoTick(object state)
        {
            ProcInfoUpdate?.Invoke(null, null);
        }

        private void PInfoUpdate(object sender, EventArgs e)
        {
            BaseProcess.Refresh();
            if(BaseProcess.HasExited)
            {
                ProcInfoTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
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
            NativeStealth.ReadProcessMemory(Handle, absoluteAddress, data, NumBytes, ref bytesRead);
            if (bytesRead != NumBytes) throw new InvalidOperationException($"Failed to read data of size {NumBytes} from address 0x{absoluteAddress}");
            return data;
        }

        public void SetBytes(PointerEx absoluteAddress, byte[] data) 
        {
            if (!Handle) throw new InvalidOperationException("Tried to write to a memory region when a handle to the desired process doesn't exist");
            PointerEx bytesWritten = IntPtr.Zero;
            NativeStealth.VirtualProtectEx(Handle, absoluteAddress, data.Length, (int)Native.MemoryProtection.ExecuteReadWrite, out int oldProtection);
            NativeStealth.WriteProcessMemory(Handle, absoluteAddress, data, data.Length, ref bytesWritten);
            NativeStealth.VirtualProtectEx(Handle, absoluteAddress, data.Length, oldProtection, out int _);
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

        /// <summary>
        /// Manual map a module into the target process' context
        /// </summary>
        /// <param name="moduleData"></param>
        /// <param name="loadOptions"></param>
        /// <returns></returns>
        public PointerEx MapModule(Memory<byte> moduleData, ModuleLoadOptions loadOptions = MLO_None)
        {
            if (BaseProcess.HasExited) throw new InvalidOperationException("Cannot inject a dll to a process which has exited");
            if (moduleData.IsEmpty) throw new ArgumentException("Cannot inject an empty dll");
            if (!Environment.Is64BitProcess && (GetArchitecture() == Architecture.X64)) throw new InvalidOperationException("Cannot map to target; a 32-bit injector cannot inject to a 64 bit process.");
            var sModule = new PEImage(moduleData);
            PointerEx hModule = NativeStealth.VirtualAllocEx(Handle, 0, (uint)sModule.Headers.PEHeader.SizeOfImage, Native.AllocationType.Commit | Native.AllocationType.Reserve, Native.MemoryProtection.ReadOnly);

            if(!hModule)
            {
                throw new Exception("Unable to allocate enough memory to map the module");
            }

            try
            {
                MMLoadDependencies(sModule, hModule);
            }
            catch
            {
                if(Handle)
                {
                    VirtualFreeEx(Handle, hModule, (uint)sModule.Headers.PEHeader.SizeOfImage, (int)FreeType.Release);
                }
                throw;
            }

            return hModule; // failed to load
        }

        public Architecture GetArchitecture()
        {
            if (!Environment.Is64BitOperatingSystem || IsWow64Process()) return Architecture.X86;
            return Architecture.X64;
        }

        internal bool IsWow64Process()
        {
            if(!IsWow64Process(BaseProcess.Handle, out bool result)) throw new ComponentModel.Win32Exception();
            return result;
        }

        public int PointerSize()
        {
            return GetArchitecture() == Architecture.X86 ? sizeof(uint) : sizeof(ulong);
        }

        /// <summary>
        /// Call a remote procedure, with a return type. Arguments are not passed by reference, and may not be manipulated by the calling process. Structs are shallow copy. Will await return signal.
        /// </summary>
        /// <param name="absoluteAddress"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Call<T>(PointerEx absoluteAddress, params object[] args)
        {
            return __CallAsync<T>(absoluteAddress, DefaultRPCType, null, args).Result;
        }

        /// <summary>
        /// Call a remote procedure, with a return type. Arguments are not passed by reference, and may not be manipulated by the calling process. Structs are shallow copy. Will await return signal.
        /// </summary>
        /// <param name="absoluteAddress"></param>
        /// <param name="callType">Type of call to initiate. Some call types must be initialized to be used.</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T CallByMethod<T>(PointerEx absoluteAddress, ExCallThreadType callType, params object[] args)
        {
            return __CallAsync<T>(absoluteAddress, callType, null, args).Result;
        }

        /// <summary>
        /// Call a remote procedure, with a return type. Arguments are passed by array reference, and the modified array will be the resultant params from proc. Structs are shallow copy. Will await return signal.
        /// </summary>
        /// <param name="absoluteAddress"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T CallRef<T>(PointerEx absoluteAddress, ref object[] args)
        {
            return CallRefByMethod<T>(absoluteAddress, DefaultRPCType, ref args);
        }

        /// <summary>
        /// Call a remote procedure, with a return type. Arguments are passed by array reference, and the modified array will be the resultant params from proc. Structs are shallow copy. Will await return signal.
        /// </summary>
        /// <param name="absoluteAddress"></param>
        /// <param name="callType">Type of call to initiate. Some call types must be initialized to be used.</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T CallRefByMethod<T>(PointerEx absoluteAddress, ExCallThreadType callType, ref object[] args)
        {
            if(args == null)
            {
                args = new object[0];
            }
            RPCParams rpcData = new RPCParams();
            rpcData.ParamData = new object[args.Length];
            if(args.Length > 0)
            {
                args.CopyTo(rpcData.ParamData, 0);
            }
            var result = __CallAsync<T>(absoluteAddress, callType, rpcData, args).Result;
            if (args.Length > 0)
            {
                rpcData.ParamData.CopyTo(args, 0);
            }
            return result;
        }

        /// <summary>
        /// Call a remote procedure, with a return type. Arguments are not passed by reference, and may not be manipulated by the calling process. Structs are shallow copy. Will await return signal.
        /// </summary>
        /// <param name="absoluteAddress"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<T> CallAsync<T>(PointerEx absoluteAddress, params object[] args)
        {
            return await __CallAsync<T>(absoluteAddress, DefaultRPCType, null, args);
        }

        /// <summary>
        /// Call a remote procedure, with a return type. Arguments are not passed by reference, and may not be manipulated by the calling process. Structs are shallow copy. Will await return signal.
        /// </summary>
        /// <param name="absoluteAddress"></param>
        /// <param name="args">Arguments to pass. NOTE: Pointer Types are passed by value.</param>
        /// <returns></returns>
        public async Task<T> CallAsyncByMethod<T>(PointerEx absoluteAddress, ExCallThreadType callType, params object[] args)
        {
            return await __CallAsync<T>(absoluteAddress, callType, null, args);
        }

        /// <summary>
        /// Call a remote procedure, with a return type. Arguments are not passed by reference, and may not be manipulated by the calling process. Structs are shallow copy. Will await return signal.
        /// </summary>
        /// <param name="absoluteAddress"></param>
        /// <param name="callType">Type of call to initiate. Some call types must be initialized to be used.</param>
        /// <param name="outParams">If defined, is the struct to use to pass output params back into the input array</param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task<T> __CallAsync<T>(PointerEx absoluteAddress, ExCallThreadType callType, RPCParams outParams, object[] args)
        {
            if (typeof(T) != typeof(VOID) && !RPCStackFrame.CanSerializeType(typeof(T)))
            {
                throw new InvalidCastException("Cannot cast type [" + typeof(T).GetType().Name + "] to a serializable type for RPC returns");
            }

            var pointerSize = PointerSize();
            var is64Call = pointerSize == 8;

            var xmmRetType = XMMR_NONE;
            if(typeof(T) == typeof(double))
            {
                xmmRetType = XMMR_DOUBLE;
            }
            if(typeof(T) == typeof(float))
            {
                xmmRetType = XMMR_SINGLE;
            }

            RPCStackFrame stackFrame = new RPCStackFrame(pointerSize);
            foreach (var arg in args)
            {
                stackFrame.PushArgument(arg);
            }

            PointerEx stackSize = stackFrame.Size();
            PointerEx hStack = QuickAlloc(stackSize);
            try
            {
                byte[] stackData = stackFrame.Build(hStack);
                var raxStorAddress = stackFrame.RAXStorOffset + hStack;
                var threadStateAddress = stackFrame.ThreadStateOffset + hStack;
                SetBytes(hStack, stackData);

                // Next, assemble the call code
                PointerEx[] ArgumentList = new PointerEx[args.Length];
                byte xmmArgMask = 0;

                for (int i = 0; i < args.Length; i++)
                {
                    ArgumentList[i] = stackFrame.GetArg(i);
                    if(is64Call && i < 4 && stackFrame.IsArgXMM(i))
                    {
                        xmmArgMask |= (byte)(1 << i);
                        if(stackFrame.IsArgXMM64(i))
                        {
                            xmmArgMask |= (byte)(1 << (i + 4));
                        }
                    }
                }

                // Write shellcode
                byte[] shellcode = ExAssembler.CreateRemoteCall(absoluteAddress, ArgumentList, PointerSize(), raxStorAddress, threadStateAddress, xmmArgMask, xmmRetType);
                PointerEx shellSize = shellcode.Length;
                PointerEx hShellcode = QuickAlloc(shellSize, true);

                try
                {
                    // Write the data for the shellcode
                    SetBytes(hShellcode, shellcode);
#if DEV
                    System.IO.File.AppendAllText("log.txt", $"hShellcode: 0x{hShellcode:X}, hStack: 0x{hStack:X}\n");
                    System.IO.File.AppendAllText("log.txt", $"shellcode: {shellcode.Hex()}\n");
                    System.IO.File.AppendAllText("log.txt", $"stack: {stackData.Hex()}\n");
                    //Environment.Exit(0);
#endif

                    switch(callType)
                    {
                        case ExCallThreadType.XCTT_NtCreateThreadEx:
                            {
                                // Start remote thread and await its exit.
                                StartThread(hShellcode, out SafeWaitHandle threadHandle);
                                AwaitThreadExit(ref threadHandle);
                            }
                            break;
                        case ExCallThreadType.XCTT_RIPHijack:
                            {
                                await CallRipHijack(hShellcode, threadStateAddress);
                            }
                            break;
                    }

                    if (!Handle) throw new Exception("Process exited unexpectedly...");

                    if (typeof(T) == typeof(VOID)) return default;

                    // read return value
                    PointerEx r_val = (PointerSize() == 4 ? GetValue<uint>(raxStorAddress) : GetValue<ulong>(raxStorAddress));

                    // deserialize args for ref calls
                    if(outParams != null && outParams.ParamData.Length > 0)
                    {
                        for(int i = 0; i < outParams.ParamData.Length; i++)
                        {
                            // only deserialize ref types
                            if (!stackFrame.IsArgByRef(i)) continue;

                            var __type = outParams.ParamData[i].GetType();
                            var __handle = stackFrame.GetArg(i);

                            if(__type == typeof(string))
                            {
                                outParams.ParamData[i] = GetString(__handle);
                                continue;
                            }

                            var __bytes = GetBytes(__handle, Marshal.SizeOf(__type));
                            outParams.ParamData[i] = __bytes.ToStruct(__type);
                        }
                    }

                    // deserialize return value to expected type

                    // if its a string...
                    if(typeof(T) == typeof(string))
                    {
                        return (T)(dynamic)(r_val ? GetString(r_val) : "");
                    }

                    // if its a value type that fits in a pointerex...
                    if(Marshal.SizeOf(default(T)) <= Marshal.SizeOf(default(PointerEx)))
                    {
                        try
                        {
                            return (T)(dynamic)r_val;
                        }
                        catch { }
                    }

                    if(r_val)
                    {
                        byte[] data = GetBytes(r_val, Marshal.SizeOf(default(T)));
                        return data.ToStructUnsafe<T>();
                    }
                }
                finally
                {
                    if (Handle)
                    {
                        VirtualFreeEx(Handle, hShellcode, shellSize, (int)FreeType.Release);
                    }
                }
            }
            finally
            {
                if(Handle)
                {
                    VirtualFreeEx(Handle, hStack, stackSize, (int)FreeType.Release);
                }
            }
            return default(T);
        }

        private async Task CallRipHijack(PointerEx hShellcode, PointerEx threadStateAddress)
        {
            var targetThread = GetEarliestActiveThread();
            if(targetThread == null)
            {
                throw new Exception("Unable to find a thread which can be hijacked.");
            }

            var hThreadResume = QuickAlloc(4096, true);
            var hXmmSpace = QuickAlloc(256 * 2, true);
            try
            {
                if (!__hijackMutexTable.ContainsKey(BaseProcess.Id))
                {
                    __hijackMutexTable[BaseProcess.Id] = new object();
                }

                lock (__hijackMutexTable[BaseProcess.Id])
                {
                    PointerEx hThread = NativeStealth.OpenThread((int)ThreadAccess.THREAD_HIJACK, false, targetThread.Id);

                    if (!hThread)
                    {
                        throw new Exception("Unable to open target thread for RPC...");
                    }

                    NativeStealth.SuspendThread(hThread);

                    if(GetArchitecture() == Architecture.X86)
                    {

                        ThreadContext32Ex ctx32 = new ThreadContext32Ex(ThreadContextExFlags.All);
                        if(!ctx32.GetContext(hThread))
                        {
                            throw new Exception("Unable to get a thread context for the target process RPC.");
                        }

                        HijackRipInternal32(hThread, hShellcode, hThreadResume, ctx32);
                        ctx32.SetContext(hThread);
                    }
                    else
                    {
                        ThreadContext64Ex ctx64 = new ThreadContext64Ex(ThreadContextExFlags.All);

                        if (!ctx64.GetContext(hThread))
                        {
                            throw new Exception("Unable to get a thread context for the target process RPC.");
                        }

                        HijackRipInternal64(hThread, hShellcode, hThreadResume, hXmmSpace, ctx64);
                        ctx64.SetContext(hThread);
                    }

                    NativeStealth.ResumeThread(hThread);
                    CloseHandle(hThread);
                }

#if DEV
                System.IO.File.AppendAllText("log.txt", $"awaiting thread exit...\n");
#endif
                // necessary wait, buffer time for something, without it, rpc hangs...
                Thread.Sleep(1);

                // await thread exit status
                while (Handle)
                {
                    if (GetValue<int>(threadStateAddress) != 0)
                    {
                        break;
                    }
                    await Task.Delay(1);
                }

#if DEV
                System.IO.File.AppendAllText("log.txt", $"good to go!\n");
#endif
            }
            finally
            {
                if(Handle)
                {
                    VirtualFreeEx(Handle, hThreadResume, 4096, (int)FreeType.Release);
                    VirtualFreeEx(Handle, hXmmSpace, 256 * 2, (int)FreeType.Release);
                }
            }
            
        }

        private void HijackRipInternal64(PointerEx hThread, PointerEx hShellcode, PointerEx hIntercept, PointerEx hXmmSpace, ThreadContext64Ex threadContext)
        {
            PointerEx originalIp = threadContext.InstructionPointer;
            byte[] data = ExAssembler.CreateThreadIntercept64(hShellcode, originalIp, hXmmSpace);
            SetBytes(hIntercept, data);
#if DEV
            System.IO.File.AppendAllText("log.txt", $"ripIntercept: {data.Hex()}\nhRipIntercept: {hIntercept:X}\nBase: {BaseAddress:X}\nOriginalIP: {originalIp:X}\n");
#endif
            threadContext.InstructionPointer = hIntercept;
        }

        private void HijackRipInternal32(PointerEx hThread, PointerEx hShellcode, PointerEx hIntercept, ThreadContext32Ex threadContext)
        {
            PointerEx originalIp = threadContext.InstructionPointer;
            byte[] data = ExAssembler.CreateThreadIntercept32(hShellcode, originalIp);
            SetBytes(hIntercept, data);
            threadContext.InstructionPointer = hIntercept;
        }

        /// <summary>
        /// Allocate readable and writable memory in the target process. If executable is true, it will also be executable. Is not managed and can be leaked, so remember to free the memory when it is no longer needed.
        /// </summary>
        /// <param name="size_region"></param>
        /// <param name="Executable"></param>
        /// <returns></returns>
        public PointerEx QuickAlloc(PointerEx size_region, bool Executable = false)
        {
            if (!Handle) throw new InvalidOperationException("Tried to allocate a memory region when a handle to the desired process doesn't exist");
            return NativeStealth.VirtualAllocEx(Handle, 0, size_region, Native.AllocationType.Commit, Executable ? Native.MemoryProtection.ExecuteReadWrite : Native.MemoryProtection.ReadWrite);
        }

        /// <summary>
        /// Start a thread in this process, at the given address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public void StartThread(PointerEx address, out SafeWaitHandle threadHandle)
        {
            if (!Handle) throw new InvalidOperationException("Tried to create a thread in a process which has no open handle!");

            var status = NtCreateThreadEx(out threadHandle, AccessMask.SpecificRightsAll | AccessMask.StandardRightsAll, IntPtr.Zero, Handle, address, IntPtr.Zero, ThreadCreationFlags.HideFromDebugger | ThreadCreationFlags.SkipThreadAttach, 0, 0, 0, IntPtr.Zero);
            if (status != 0)
            {
                throw new Win32Exception(RtlNtStatusToDosError(status));
            }
        }

        /// <summary>
        /// Await the exit of a thread by its handle, optionally declaring the maximum time to wait.
        /// </summary>
        /// <param name="threadHandle"></param>
        /// <param name="MaxMSWait"></param>
        public void AwaitThreadExit(ref SafeWaitHandle threadHandle, int MaxMSWait = int.MaxValue)
        {
            if (threadHandle == null || threadHandle.IsClosed) return;
            using (threadHandle)
            {
                if (WaitForSingleObject(threadHandle, MaxMSWait) == -1)
                {
                    throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// Apply a new default calling type to RPC for this process. The type specified must be initialized prior to changing the default.
        /// </summary>
        /// <param name="type"></param>
        public void SetDefaultCallType(ExCallThreadType type)
        {
            if (!IsRPCTypeInitialized(type))
                throw new InvalidOperationException($"Cannot invoke rpc of type {type} because the rpc type has not been initialized.");
            DefaultRPCType = type;
        }

        /// <summary>
        /// Determines if an RPC type has been initialized properly
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsRPCTypeInitialized(ExCallThreadType type)
        {
            switch(type)
            {
                case ExCallThreadType.XCTT_RIPHijack:
                case ExCallThreadType.XCTT_NtCreateThreadEx:
                    return true;

                default:
                    throw new NotImplementedException($"Calltype {type} not implemented");
            }
        }

        /// <summary>
        /// Find the active thread with the earliest creation time
        /// </summary>
        /// <returns></returns>
        public ProcessThread GetEarliestActiveThread()
        {
            if (BaseProcess.HasExited) return null;
            ProcessThread earliest = null;
            foreach(ProcessThread thread in BaseProcess.Threads)
            {
                if (thread.ThreadState == Diagnostics.ThreadState.Terminated) continue;
                if (earliest == null)
                {
                    earliest = thread;
                    continue;
                }
                if (thread.StartTime < earliest.StartTime)
                {
                    earliest = thread;
                }
            }
            return earliest;
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

        /// <summary>
        /// The default code execution type for an RPC call with no thread type specifier
        /// </summary>
        public ExCallThreadType DefaultRPCType { get; private set; }

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

        #region static members
        private static Dictionary<int, object> __hijackMutexTable = new Dictionary<int, object>();
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

    public enum ExCallThreadType
    {
        /// <summary>
        /// Start the call thread via NtCreateThreadEx (thread safe, default)
        /// </summary>
        XCTT_NtCreateThreadEx,

        /// <summary>
        /// Start a call via a thread hijacking procedure involving an RIP detour via the thread id specified (not thread safe)
        /// </summary>
        XCTT_RIPHijack,

        /// <summary>
        /// Start a call via a vectored exception (not thread safe)
        /// </summary>
        XCTT_VEH,

        /// <summary>
        /// Start a call via a VMT detour (not thread safe)
        /// </summary>
        XCTT_VMT_Detour,

        /// <summary>
        /// Start a call via a pointer replacement, typically in the data section
        /// </summary>
        XCTT_Custom_DS_Detour
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

    /// <summary>
    /// Params object for RPC output
    /// </summary>
    internal class RPCParams
    {
        public object[] ParamData;
    }
    #endregion
    #endregion
}
