using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public enum MemorySearchFlags
    {
        Read = 2,
        Write = 4,
        Execute = 8,
        RW = 2 | 4,
        RX = 2 | 8,
        RWX = 2 | 4 | 8
    }

    internal sealed class MemorySearcher
    {
        private readonly ProcessEx Proc;
        public MemorySearcher(ProcessEx hostProcess)
        {
            Proc = hostProcess;
        }

        private struct SearcherResult
        {
            public PointerEx BaseAddress { get; set; }
            public PointerEx RegionSize { get; set; }
            public PointerEx RegionBase { get; set; }
        }

        // credit: https://github.com/erfg12/memory.dll/blob/master/Memory/memory.cs
        public Task<IEnumerable<PointerEx>> Search(string query, PointerEx start, PointerEx end, MemorySearchFlags flags)
        {
            return Task.Run(() =>
            {
                var Results = new List<SearcherResult>();
                string[] patterns = query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                byte[] finalPattern = new byte[patterns.Length];
                byte[] mask = new byte[patterns.Length];
                for(int i = 0; i < patterns.Length; i++)
                {
                    string s = patterns[i];
                    if (s == "?") mask[i] = 0;
                    else if (s.Length < 2)
                    {
                        if (s.HexByte(out byte b)) finalPattern[i] = b;
                        mask[i] = 0xFF;
                    }
                    else
                    {
                        mask[i] = (byte)((s[0].IsHex() ? 0xF0 : 0) + (s[1].IsHex() ? 0x0F : 0));
                        if ((mask[i] & 0xF0) > 0) finalPattern[i] += (byte)(s[0].HexByte() * 0x10);
                        if ((mask[i] & 0xF) > 0) finalPattern[i] += s[1].HexByte();
                    }
                }
                var sysInfo = new ProcessEx.SYSTEM_INFO();
                ProcessEx.GetSystemInfo(out sysInfo);
                PointerEx proc_min_address = sysInfo.lpMinimumApplicationAddress;
                PointerEx proc_max_address = sysInfo.lpMaximumApplicationAddress;
                if (start < proc_min_address) start = proc_min_address;
                if (end > proc_max_address) end = proc_max_address;
                PointerEx cBase = start.Clone();
                var memInfo = new ProcessEx.MEMORY_BASIC_INFORMATION();
                while(ProcessEx.VirtualQueryEx(Proc.Handle, cBase, out memInfo, (uint)Marshal.SizeOf(memInfo)) &&
                cBase < end && cBase + memInfo.RegionSize > cBase)
                {
                    bool isValid = memInfo.State == ProcessEx.MEM_COMMIT;
                    isValid &= memInfo.BaseAddress < proc_max_address;
                    isValid &= ((memInfo.Protect & Native.PAGE_GUARD) == 0);
                    isValid &= ((memInfo.Protect & Native.PAGE_NOACCESS) == 0);
                    isValid &= (memInfo.Type == ProcessEx.MEM_PRIVATE) || (memInfo.Type == ProcessEx.MEM_IMAGE);
                    if (isValid)
                    {
                        bool isReadable = (memInfo.Protect & Native.PAGE_READONLY) > 0;
                        bool isWritable = ((memInfo.Protect & Native.PAGE_READWRITE) > 0) ||
                                          ((memInfo.Protect & Native.PAGE_WRITECOPY) > 0) ||
                                          ((memInfo.Protect & Native.PAGE_EXECUTE_READWRITE) > 0) ||
                                          ((memInfo.Protect & Native.PAGE_EXECUTE_WRITECOPY) > 0);
                        bool isExecutable = ((memInfo.Protect & Native.PAGE_EXECUTE) > 0) ||
                                            ((memInfo.Protect & Native.PAGE_EXECUTE_READ) > 0) ||
                                            ((memInfo.Protect & Native.PAGE_EXECUTE_READWRITE) > 0) ||
                                            ((memInfo.Protect & Native.PAGE_EXECUTE_WRITECOPY) > 0);
                        isReadable &= ((byte)flags & (byte)MemorySearchFlags.Read) > 0;
                        isWritable &= ((byte)flags & (byte)MemorySearchFlags.Write) > 0;
                        isExecutable &= ((byte)flags & (byte)MemorySearchFlags.Execute) > 0;
                        isValid &= isReadable || isWritable || isExecutable;
                    }
                    cBase = memInfo.BaseAddress + memInfo.RegionSize;
                    if (!isValid)
                    {
                        continue;
                    }
                    SearcherResult result = new SearcherResult
                    {
                        BaseAddress = cBase - memInfo.RegionSize,
                        RegionSize = memInfo.RegionSize,
                        RegionBase = memInfo.BaseAddress
                    };
                    if (Results.Count > 0)
                    {
                        var previousRegion = Results[Results.Count - 1];
                        if ((previousRegion.RegionBase + previousRegion.RegionSize) == memInfo.BaseAddress)
                        {
                            Results[Results.Count - 1] = new SearcherResult
                            {
                                BaseAddress = previousRegion.BaseAddress,
                                RegionBase = previousRegion.RegionBase,
                                RegionSize = previousRegion.RegionSize + memInfo.RegionSize
                            };
                            continue;
                        }
                    }
                    Results.Add(result);
                } // end while
                ConcurrentBag<PointerEx> bagResult = new ConcurrentBag<PointerEx>();
                Parallel.ForEach(Results,
                    (item, parallelLoopState, index) =>
                    {
                        PointerEx[] compareResults = CompareScan(item, finalPattern, mask);
                        foreach (PointerEx result in compareResults) bagResult.Add(result);
                    });
                return bagResult.ToList().OrderBy(c => c).AsEnumerable();
            });
        }

        private PointerEx[] CompareScan(SearcherResult item, byte[] finalPattern, byte[] mask)
        {
            Debug.Assert(mask.Length == finalPattern.Length);
            var buffer = Proc.GetBytes(item.BaseAddress, item.RegionSize);
            int result = -finalPattern.Length;
            List<PointerEx> ret = new List<PointerEx>();
            unsafe
            {
                do
                {
                    result = FindPattern(buffer, finalPattern, mask, result + finalPattern.Length);
                    if (result >= 0) ret.Add((long)item.BaseAddress + result);
                } while (result != -1);
            }
            return ret.ToArray();
        }

        private int FindPattern(byte[] body, byte[] pattern, byte[] masks, int start = 0)
        {
            int foundIndex = -1;
            if (body.Length <= 0 || pattern.Length <= 0 ) return -1;
            if (start > body.Length - pattern.Length || pattern.Length > body.Length) return -1;
            for (int index = start; index <= body.Length - pattern.Length; index++)
            {
                if ((body[index] & masks[0]) == (pattern[0] & masks[0]))
                {
                    var match = true;
                    for (int index2 = 1; index2 <= pattern.Length - 1; index2++)
                    {
                        if ((body[index + index2] & masks[index2]) == (pattern[index2] & masks[index2])) continue;
                        match = false;
                        break;
                    }
                    if (!match) continue;
                    foundIndex = index;
                    break;
                }
            }
            return foundIndex;
        }
    }
}
