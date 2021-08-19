using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace T7MemUtil
{
    /// <summary>
    /// Deprecated in favor of using external struct parsing
    /// </summary>
    public static class T7Memory
    {
        internal const int PROCESS_ACCESS = PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PAGE_READWRITE = 0x04;
        const int PROCESS_VM_READ = 0x0010;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int MEM_IMAGE = 0x1000000;
        const int GSC_REGIONSIZE = 0x2E0A0000;
        const int GSC_REGIONOFFSET = 0x15F380;
        const int SPT_REGIONSIZE = 0x1FAAF000;
        const int SPT_REGIONOFFSET = 0x8C89A90; //note: this *should* actually be spt + 0x10 because we are actually searching for the buffer offset
        private const uint PROCESS_STILL_ACTIVE = 259;

        private const string PROCESS_NAME = "BlackOps3.exe";

        private static readonly byte[] GSC_MAGIC = new byte[] { 0x80, 0x47, 0x53, 0x43, 0x0D, 0x0A, 0x00 };
        private static readonly byte[] GSC_SECTION_MAGIC = new byte[] { 0x50, 0x58, 0x16, 0x6F, 0xF7, 0x7F, 0x00, 0x00 };

        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, IntPtr dwSize, ref IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetExitCodeProcess(IntPtr hProcess, out uint ExitCode);

        [DllImport("kernel32.dll")]
        internal static extern int GetProcessId(IntPtr handle);

        [DllImport("kernel32.dll")]
        internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualQueryEx(IntPtr hProcess,
        IntPtr lpAddress, out MEMORY_BASIC_INFORMATION_64 lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualAlloc2(IntPtr hProcess, IntPtr lpBaseAddress, int RegionSize, ulong AllocType, ulong PageProtection);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
           uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int dwFreeType);

        internal const int MEM_DECOMMIT = 0x4000;

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


        public struct MEMORY_BASIC_INFORMATION_64
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_INFO
        {
            internal ushort wProcessorArchitecture;
            internal ushort wReserved;
            internal uint dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal uint dwNumberOfProcessors;
            internal uint dwProcessorType;
            internal uint dwAllocationGranularity;
            internal ushort wProcessorLevel;
            internal ushort wProcessorRevision;
        }

        public struct ScriptParseTreeEntry
        {
            public IntPtr EntryLocation;
            public IntPtr NameOffset;
            public IntPtr BufferSize;
            public IntPtr BufferLocation;
            public uint Crc32;
            public string Name;
        }

        public static void Main(string[] args)
        {
        }

        public static bool LocateProcessInformation()
        {
            if (ProcessHandle == default)
                return false;

            if (THIS_SYSTEM_INFO.lpMaximumApplicationAddress == default)
                return false;

            return true;
        }

        private static SYSTEM_INFO __sysinfointernal__;
        private static SYSTEM_INFO THIS_SYSTEM_INFO
        {
            get
            {
                if (__sysinfointernal__.lpMaximumApplicationAddress != default)
                    return __sysinfointernal__;

                __sysinfointernal__ = new SYSTEM_INFO();
                GetSystemInfo(out __sysinfointernal__);

                return __sysinfointernal__;
            }
        }


        public static IntPtr GSCMemoryRegionStartAddress
        { get; private set; }

        public static IntPtr GSCMemoryRegionSize
        { get; private set; }

        public static IntPtr ImageMemoryRegionStart
        { get; private set; }

        public static IntPtr ImageMemoryRegionSize
        { get; private set; }

        private static IntPtr __firstgsc__;
        public static IntPtr FirstGSCScript
        {
            get
            {
                if (__firstgsc__ != default)
                    return __firstgsc__;

                return __firstgsc__ = LocateRegion(RegionType.GSC_REGION);
            }
        }

        private static IntPtr __spt__;
        public static IntPtr ScriptParseTreeLocation
        {
            get
            {
                if (__spt__ != default)
                    return __spt__;

                return __spt__ = LocateRegion(RegionType.SCRIPT_PARSETREE);
            }
        }

        private enum RegionType
        {
            GSC_REGION,
            SCRIPT_PARSETREE
        }

        private static IntPtr LocateRegion(RegionType region)
        {

            if (ProcessHandle == default)
                return default;

            IntPtr proc_min_address_l = THIS_SYSTEM_INFO.lpMinimumApplicationAddress;
            IntPtr proc_max_address_l = THIS_SYSTEM_INFO.lpMaximumApplicationAddress;

            MEMORY_BASIC_INFORMATION_64 mem_basic_info;

            switch (region)
            {
                case RegionType.GSC_REGION:
                    while ((ulong)proc_min_address_l.ToInt64() < (ulong)proc_max_address_l.ToInt64())
                    {
                        VirtualQueryEx(ProcessHandle, proc_min_address_l, out mem_basic_info, 48);

                        if ((mem_basic_info.Protect == PAGE_READWRITE) && mem_basic_info.RegionSize.ToInt64() >= GSC_REGIONSIZE) //GSC_REGIONSIZE == mem_basic_info.RegionSize.ToInt64() &&
                        {
                            byte[] ExpectedMagic = new byte[8];
                            ReadT7Memory(mem_basic_info.BaseAddress + GSC_REGIONOFFSET, ref ExpectedMagic);
                            for (int i = 0; i < Math.Min(GSC_MAGIC.Length, ExpectedMagic.Length); i++)
                            {
                                if (GSC_MAGIC[i] != ExpectedMagic[i])
                                    goto EndOfLoop;

                            }
                            GSCMemoryRegionStartAddress = mem_basic_info.BaseAddress;
                            GSCMemoryRegionSize = mem_basic_info.RegionSize;
                            //Console.WriteLine($"BASE: 0x{mem_basic_info.BaseAddress.ToInt64().ToString("X")}; REGION SIZE: 0x{mem_basic_info.RegionSize.ToInt64().ToString("X")}; ");

                            return mem_basic_info.BaseAddress + GSC_REGIONOFFSET;
                            //Found our GSC region.
                            //return FirstGSCLocationInternal(mem_basic_info.BaseAddress + GSC_REGIONOFFSET, new IntPtr(mem_basic_info.BaseAddress.ToInt64() + mem_basic_info.RegionSize.ToInt64()));
                        }
                    EndOfLoop:
                        proc_min_address_l = new IntPtr((long)proc_min_address_l + (long)mem_basic_info.RegionSize);
                    }
                    return default;

                case RegionType.SCRIPT_PARSETREE:

                    if (FirstGSCScript == default) //Find the GSC script first.
                        return default;

                    while ((ulong)proc_min_address_l.ToInt64() < (ulong)proc_max_address_l.ToInt64())
                    {
                        VirtualQueryEx(ProcessHandle, proc_min_address_l, out mem_basic_info, 48);

                        if (mem_basic_info.RegionSize.ToInt64() == SPT_REGIONSIZE && (mem_basic_info.Type & MEM_IMAGE) > 0)
                        {
                            ImageMemoryRegionStart = mem_basic_info.BaseAddress;
                            ImageMemoryRegionSize = mem_basic_info.RegionSize;
                            //Console.WriteLine($"IMAGE BASE: 0x{mem_basic_info.BaseAddress.ToInt64().ToString("X")}; REGION SIZE: 0x{mem_basic_info.RegionSize.ToInt64().ToString("X")}; ");
                            return LocateSPTInternal(mem_basic_info.BaseAddress + SPT_REGIONOFFSET, new IntPtr(mem_basic_info.BaseAddress.ToInt64() + mem_basic_info.RegionSize.ToInt64()));
                        }

                        proc_min_address_l = new IntPtr((long)proc_min_address_l + (long)mem_basic_info.RegionSize);
                    }
                    return default;
            }

            return default;
        }

        private static IntPtr LocateSPTInternal(IntPtr StartAddress, IntPtr MaxAddress)
        {
            IntPtr CurrentAddress = new IntPtr(StartAddress.ToInt64());

            byte[] GSCLocation = BitConverter.GetBytes(FirstGSCScript.ToInt64());
            byte[] buffer = new byte[GSCLocation.Length];

            while (CurrentAddress.ToInt64() < MaxAddress.ToInt64())
            {
                if (ReadT7Memory(CurrentAddress, ref buffer) != MemoryOperationResult.SUCCESS)
                    Console.WriteLine($"FAILED READ AT: 0x{CurrentAddress.ToInt64().ToString("X")}");

                //Console.WriteLine($"CURRENT (SPT SEARCHER): 0x{CurrentAddress.ToInt64().ToString("X")} = {String.Join(",", buffer)}");

                for (int i = 0; i < GSCLocation.Length; i++)
                {
                    if (buffer[i] != GSCLocation[i])
                    {
                        goto EndOfLoop;
                    }

                }

                return CurrentAddress - 0x10;

            EndOfLoop:
                CurrentAddress += 0x10;
            }

            return default;
        }

        private static IntPtr FirstGSCLocationInternal(IntPtr StartAddress, IntPtr MaxAddress)
        {
            IntPtr CurrentAddress = new IntPtr(StartAddress.ToInt64());

            byte[] buffer = new byte[GSC_MAGIC.Length];

            while (CurrentAddress.ToInt64() < MaxAddress.ToInt64())
            {
                if (ReadT7Memory(CurrentAddress, ref buffer) != MemoryOperationResult.SUCCESS)
                    Console.WriteLine($"FAILED READ AT: 0x{CurrentAddress.ToInt64().ToString("X")}");

                //Console.WriteLine($"CURRENT: 0x{CurrentAddress.ToInt64().ToString("X")} = {String.Join(",", buffer)}");

                for (int i = 0; i < GSC_MAGIC.Length; i++)
                {
                    if (buffer[i] != GSC_MAGIC[i])
                    {
                        goto EndOfLoop;
                    }

                }

                return CurrentAddress;

            EndOfLoop:
                CurrentAddress += 0x10;
            }

            return default;
        }

        private static IntPtr __processhandleintern__ = default;

        /// <summary>
        /// Attach the process and store the handle. If we already have a handle, and the process is valid, use the cached process.
        /// </summary>
        /// <returns></returns>
        private static IntPtr ProcessHandle
        {
            get
            {
                if (__processhandleintern__ != default)
                    if (GetExitCodeProcess(__processhandleintern__, out uint ExitCode))
                        if (ExitCode == PROCESS_STILL_ACTIVE)
                            return __processhandleintern__;

                __processhandleintern__ = default;

                Process[] processes = Process.GetProcessesByName("BlackOps3");

                if (processes.Length < 1)
                    return default;

                try
                {
                    __processhandleintern__ = OpenProcess(PROCESS_ACCESS, false, processes[0].Id);

                    if (GetExitCodeProcess(__processhandleintern__, out uint ExitCode))
                        if (ExitCode == PROCESS_STILL_ACTIVE)
                            return __processhandleintern__;

                }
                catch (Exception)
                { return default; }

                return default;
            }
        }

        /// <summary>
        /// Enumeration of memory results.
        /// </summary>
        public enum MemoryOperationResult
        {
            SUCCESS,
            FAILED_BADPROCESS,
            FAILED_BADREAD,
            FAILED_BADWRITE
        }

        /// <summary>
        /// Attempts to read T7 memory. Will read the amount of memory requested in the buffer, at the requested address.
        /// </summary>
        /// <param name="Address">Address to read</param>
        /// <param name="buffer">Output buffer for memory data</param>
        /// <returns></returns>
        public static MemoryOperationResult ReadT7Memory(IntPtr Address, ref byte[] buffer)
        {
            if (ProcessHandle == default)
                return MemoryOperationResult.FAILED_BADPROCESS; //Couldnt read an inactive process

            IntPtr NumBytesRead = default;

            ReadProcessMemory(ProcessHandle, Address, buffer, (IntPtr)buffer.Length, ref NumBytesRead);

            if ((ulong)NumBytesRead != (ulong)buffer.Length)
                return MemoryOperationResult.FAILED_BADREAD;

            return MemoryOperationResult.SUCCESS;
        }

        public static MemoryOperationResult WriteT7Memory(IntPtr Address, byte[] data)
        {
            if (ProcessHandle == default)
                return MemoryOperationResult.FAILED_BADPROCESS; //Couldnt write to an inactive process

            int NumWritten = 0;
            bool result = WriteProcessMemory(ProcessHandle, Address, data, data.Length, ref NumWritten);

            if (!result || NumWritten != data.Length)
                return MemoryOperationResult.FAILED_BADWRITE;

            return MemoryOperationResult.SUCCESS;
        }

        public static bool ReadT7UInt32(IntPtr Address, ref uint Out)
        {
            byte[] buffer = new byte[sizeof(uint)];

            MemoryOperationResult r = ReadT7Memory(Address, ref buffer);

            if (r != MemoryOperationResult.SUCCESS)
                return false;

            Out = BitConverter.ToUInt32(buffer, 0);

            return true;
        }

        public static bool ReadT7IntPtr(IntPtr Address, ref IntPtr ptr)
        {
            byte[] buffer = new byte[sizeof(long)];

            MemoryOperationResult r = ReadT7Memory(Address, ref buffer);

            if (r != MemoryOperationResult.SUCCESS)
                return false;

            ptr = new IntPtr(BitConverter.ToInt64(buffer, 0));

            return true;
        }

        private const int WalkSize = 0x4;
        public static bool ReadT7CString(IntPtr Address, ref string str, int MaxSize = 256)
        {
            IntPtr current = Address;

            StringBuilder b = new StringBuilder();

            byte[] buffer = new byte[WalkSize];

            MemoryOperationResult r = MemoryOperationResult.SUCCESS;

            int Numread = 0;
            while ((r = ReadT7Memory(current + Numread, ref buffer)) == MemoryOperationResult.SUCCESS)
            {

                do
                {
                    if (buffer[Numread % WalkSize] == 0x0)
                        goto BufferFinished;

                    b.Append((char)buffer[Numread++ % WalkSize]);

                } while (Numread % WalkSize != 0);

                if (MaxSize <= Numread)
                    break;
            }

            return false;

        BufferFinished:
            str = b.ToString();

            return true;
        }

        private static Dictionary<string, ScriptParseTreeEntry> ParseTreeCache = new Dictionary<string, ScriptParseTreeEntry>();

        public static bool UpdateTreeInfo()
        {
            return ReadScriptParseTreeEntries().Length > 0;
        }

        public static ScriptParseTreeEntry[] ReadScriptParseTreeEntries()
        {
            IntPtr CurrentLocation = ScriptParseTreeLocation;

            if (CurrentLocation == default)
                return new ScriptParseTreeEntry[0];

            Console.WriteLine("Read Script parsetree location...");

            List<ScriptParseTreeEntry> Entries = new List<ScriptParseTreeEntry>();

            while (true)
            {
                ScriptParseTreeEntry s = new ScriptParseTreeEntry();

                s.EntryLocation = CurrentLocation;
                ReadT7IntPtr(CurrentLocation, ref s.NameOffset);
                ReadT7IntPtr(CurrentLocation + 0x8, ref s.BufferSize);
                ReadT7IntPtr(CurrentLocation + 0x10, ref s.BufferLocation);
                ReadT7UInt32(s.BufferLocation + 0x8, ref s.Crc32);

                if (s.NameOffset == default || s.BufferSize == default || s.BufferLocation == default)
                    break;

                ReadT7CString(s.NameOffset, ref s.Name);
                s.Name = s.Name.ToLower();

                ParseTreeCache[s.Name] = s;

                Entries.Add(s);
                CurrentLocation += 0x18;
            }

            return Entries.ToArray();
        }

        /// <summary>
        /// Patches a script location.
        /// </summary>
        /// <param name="scriptname"></param>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool PatchScriptLocation(string scriptname, int offset, byte[] data)
        {
            if (ProcessHandle == default)
                return false;

            if (!ParseTreeCache.ContainsKey(scriptname.ToLower()))
                return false;

            return WriteT7Memory(ParseTreeCache[scriptname].BufferLocation + offset, data) == MemoryOperationResult.SUCCESS;
        }

        internal struct CustomGSCAllocation
        {
            internal IntPtr OriginalBufferPtr;
            internal IntPtr RegionPtr;
            internal uint BufferSize;
            internal uint RegionSize;
        }

        /// <summary>
        /// List of modified GSC buffers (only from this instance)
        /// </summary>
        private static Dictionary<string, CustomGSCAllocation> AllocatedBufferInfo = new Dictionary<string, CustomGSCAllocation>();

        /// <summary>
        /// Write a GSC buffer to memory
        /// </summary>
        /// <param name="scriptname"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static bool PatchGSCScript(string scriptname, byte[] buffer)
        {
            if (buffer.Length < 0x10) //Cant emit an invalid gsc
                return false;

            if (!UpdateTreeInfo()) //Update cached info
                return false;

            //Console.WriteLine("Tree updated...");

            if (!ParseTreeCache.ContainsKey(scriptname)) //unknown script
                return false;

            //Patch the crc32 at runtime
            BitConverter.GetBytes(ParseTreeCache[scriptname].Crc32).CopyTo(buffer, 0x8);

            //Console.WriteLine("Parsetree located the script...");

            ResetGSCScript(scriptname); //De-alloc old script memory

            uint RequestedBufferSize = ((uint)buffer.Length / THIS_SYSTEM_INFO.dwPageSize + 1) * 2 * THIS_SYSTEM_INFO.dwPageSize; //Size of memory we need for this buffer
            CustomGSCAllocation c = new CustomGSCAllocation();

            c.OriginalBufferPtr = ParseTreeCache[scriptname].BufferLocation;
            c.BufferSize = (uint)buffer.Length;
            c.RegionSize = RequestedBufferSize;

            c.RegionPtr = VirtualAllocEx(ProcessHandle, default, RequestedBufferSize, AllocationType.Commit, MemoryProtection.ReadWrite);

            if (c.RegionPtr == default)
                return false; //Couldnt allocate a buffer for whatever reason

            Console.WriteLine("Allocated memory...");
            AllocatedBufferInfo[scriptname] = c;

            //Commit memory
            WriteT7Memory(c.RegionPtr, buffer);
            WriteT7Memory(ParseTreeCache[scriptname].EntryLocation + 0x8, BitConverter.GetBytes(c.BufferSize));
            WriteT7Memory(ParseTreeCache[scriptname].EntryLocation + 0x10, BitConverter.GetBytes(c.RegionPtr.ToInt64()));

            Console.WriteLine($"Injection: {scriptname} at 0x{c.RegionPtr.ToInt64().ToString("X")}, BufferSize = {c.BufferSize}, RegionSize = {RequestedBufferSize}, ModifiedEntry at: 0x{(ParseTreeCache[scriptname].EntryLocation + 0x10).ToInt64().ToString("X")}");

            return true;
        }

        /// <summary>
        /// Resets script parsetree (according to local memory). Note: will return true if the script doesnt exist in the cache.
        /// </summary>
        /// <param name="scriptname"></param>
        /// <returns></returns>
        public static bool ResetGSCScript(string scriptname)
        {
            if (ProcessHandle == default) //we have a bad process ptr
                return false;

            if (!AllocatedBufferInfo.ContainsKey(scriptname)) //No buffer to free!
                return true;

            CustomGSCAllocation c = AllocatedBufferInfo[scriptname];

            bool result = VirtualFreeEx(ProcessHandle, c.RegionPtr, c.RegionSize, MEM_DECOMMIT);

            //Reset original buffer & size
            WriteT7Memory(ParseTreeCache[scriptname].EntryLocation + 0x8, BitConverter.GetBytes(ParseTreeCache[scriptname].BufferSize.ToInt64()));
            WriteT7Memory(ParseTreeCache[scriptname].EntryLocation + 0x10, BitConverter.GetBytes(c.OriginalBufferPtr.ToInt64()));

            if (result) AllocatedBufferInfo.Remove(scriptname);

            return result;
        }

        /// <summary>
        /// Reset all gsc script buffers. I recommend binding this to application exit.
        /// </summary>
        public static void FreeAll()
        {
            string[] keys = new string[AllocatedBufferInfo.Keys.Count];

            AllocatedBufferInfo.Keys.CopyTo(keys, 0);

            foreach (string s in keys)
                ResetGSCScript(s);
        }
    }
}
