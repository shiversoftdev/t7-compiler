using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.PEStructures.PE;
using static System.EnvironmentEx;
namespace System.Evasion
{
    public static class ModuleMapper
    {
        private static readonly Dictionary<string, PE_MANUAL_MAP> MappedModulesCache = new Dictionary<string, PE_MANUAL_MAP>();

        internal static class ModuleConst
        {
            public static string CONST_KERNEL32 => ToSysDLL("kernel32.dll");
            public static string CONST_NTDLL => ToSysDLL("ntdll.dll");

            private static string ToSysDLL(string relPath)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), relPath);
            }
        }

        /// <summary>
        /// Manually map module into current process.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author> (modified for external)
        /// <param name="modulePath">Full path to the module on disk.</param>
        /// <param name="noCache">If true, will ignore any previous mappings cached locally</param>
        /// <returns>PE_MANUAL_MAP object</returns>
        public static PE_MANUAL_MAP MapModuleToMemory(string modulePath, bool noCache = false)
        {
            if (modulePath == null)
            {
                throw new Exception(DSTR(DSTR_DINVOKE_MOD_CANNOT_BE_NULL));
            }

            modulePath = modulePath.ToLower();
            if (!noCache && MappedModulesCache.ContainsKey(modulePath))
            {
                return MappedModulesCache[modulePath];
            }

            // Alloc module into memory for parsing
            PointerEx pModule = File.ReadAllBytes(modulePath).Unmanaged();
            var result = MapModuleToMemory(pModule);
            MappedModulesCache[modulePath] = result;
            return result;
        }

        /// <summary>
        /// Manually map module into current process.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author> (modified for external)
        /// <param name="pModule">Pointer to the module base.</param>
        /// <returns>PE_MANUAL_MAP object</returns>
        private static PE_MANUAL_MAP MapModuleToMemory(PointerEx pModule)
        {
            // Fetch PE meta data
            PE_META_DATA PEINFO = GetPeMetaData(pModule);

            // Check module matches the process architecture
            if ((PEINFO.Is32Bit && IntPtr.Size == 8) || (!PEINFO.Is32Bit && IntPtr.Size == 4))
            {
                Marshal.FreeHGlobal(pModule);
                throw new Exception(DSTR(DSTR_MOD_ARCHITECTURE_WRONG));
            }

            // Alloc PE image memory -> RW
            IntPtr BaseAddress = IntPtr.Zero;
            IntPtr RegionSize = PEINFO.Is32Bit ? (IntPtr)PEINFO.OptHeader32.SizeOfImage : (IntPtr)PEINFO.OptHeader64.SizeOfImage;
            IntPtr pImage = Native.NtAllocateVirtualMemoryD(
                (IntPtr)(-1), ref BaseAddress, IntPtr.Zero, ref RegionSize,
                Native.AllocationType.Commit | Native.AllocationType.Reserve,
                Native.PAGE_READWRITE
            );
            return MapModuleToMemory(pModule, pImage, PEINFO);
        }

        /// <summary>
        /// Helper for getting the pointer to a function from a DLL loaded by the process.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="DLLName">The name of the DLL (e.g. "ntdll.dll" or "C:\Windows\System32\ntdll.dll").</param>
        /// <param name="FunctionName">Name of the exported procedure.</param>
        /// <param name="CanLoadFromDisk">Optional, indicates if the function can try to load the DLL from disk if it is not found in the loaded module list.</param>
        /// <returns>IntPtr for the desired function.</returns>
        public static IntPtr GetLibraryAddress(string DLLName, string FunctionName, bool CanLoadFromDisk = false)
        {
            IntPtr hModule = GetLoadedModuleAddress(DLLName);
            if (hModule == IntPtr.Zero && CanLoadFromDisk)
            {
                hModule = LoadModuleFromDisk(DLLName);
                if (hModule == IntPtr.Zero)
                {
                    throw new Exception(DSTR(DSTR_MODULE_FILE_NOT_FOUND, DLLName));
                }
            }
            else if (hModule == IntPtr.Zero)
            {
                throw new Exception(DSTR(DSTR_MODULE_FILE_NOT_FOUND, DLLName));
            }

            return GetExportAddress(hModule, FunctionName);
        }

        private static Dictionary<string, PointerEx> cached_lookups = new Dictionary<string, PointerEx>();

        /// <summary>
        /// Helper for getting the base address of a module loaded by the current process. This base
        /// address could be passed to GetProcAddress/LdrGetProcedureAddress or it could be used for
        /// manual export parsing. This function uses the .NET System.Diagnostics.Process class.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="DLLName">The name of the DLL (e.g. "ntdll.dll").</param>
        /// <returns>IntPtr base address of the loaded module or IntPtr.Zero if the module is not found.</returns>
        public static IntPtr GetLoadedModuleAddress(string DLLName)
        {
            if(cached_lookups.ContainsKey(DLLName))
            {
                return cached_lookups[DLLName];
            }

            ProcessModuleCollection ProcModules = Process.GetCurrentProcess().Modules;
            foreach (ProcessModule Mod in ProcModules)
            {
                if (Mod.FileName.ToLower().EndsWith(DLLName.ToLower()))
                {
                    return cached_lookups[DLLName] = Mod.BaseAddress;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Resolves LdrLoadDll and uses that function to load a DLL from disk.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="DLLPath">The path to the DLL on disk. Uses the LoadLibrary convention.</param>
        /// <returns>IntPtr base address of the loaded module or IntPtr.Zero if the module was not loaded successfully.</returns>
        public static IntPtr LoadModuleFromDisk(string DLLPath)
        {
            Native.UNICODE_STRING uModuleName = new Native.UNICODE_STRING();
            Native.RtlInitUnicodeStringD(ref uModuleName, DLLPath);

            IntPtr hModule = IntPtr.Zero;
            Native.NTSTATUS CallResult = Native.LdrLoadDllD(IntPtr.Zero, 0, ref uModuleName, ref hModule);
            if (CallResult != Native.NTSTATUS.Success || hModule == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            return hModule;
        }

        /// <summary>
        /// Given a module base address, resolve the address of a function by manually walking the module export table.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="ModuleBase">A pointer to the base address where the module is loaded in the current process.</param>
        /// <param name="ExportName">The name of the export to search for (e.g. "NtAlertResumeThread").</param>
        /// <returns>IntPtr for the desired function.</returns>
        public static IntPtr GetExportAddress(IntPtr ModuleBase, string ExportName)
        {
            IntPtr FunctionPtr = IntPtr.Zero;
            try
            {
                // Traverse the PE header in memory
                Int32 PeHeader = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + 0x3C));
                Int16 OptHeaderSize = Marshal.ReadInt16((IntPtr)(ModuleBase.ToInt64() + PeHeader + 0x14));
                Int64 OptHeader = ModuleBase.ToInt64() + PeHeader + 0x18;
                Int16 Magic = Marshal.ReadInt16((IntPtr)OptHeader);
                Int64 pExport = 0;
                if (Magic == 0x010b)
                {
                    pExport = OptHeader + 0x60;
                }
                else
                {
                    pExport = OptHeader + 0x70;
                }

                // Read -> IMAGE_EXPORT_DIRECTORY
                Int32 ExportRVA = Marshal.ReadInt32((IntPtr)pExport);
                Int32 OrdinalBase = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + ExportRVA + 0x10));
                Int32 NumberOfFunctions = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + ExportRVA + 0x14));
                Int32 NumberOfNames = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + ExportRVA + 0x18));
                Int32 FunctionsRVA = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + ExportRVA + 0x1C));
                Int32 NamesRVA = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + ExportRVA + 0x20));
                Int32 OrdinalsRVA = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + ExportRVA + 0x24));

                // Loop the array of export name RVA's
                for (int i = 0; i < NumberOfNames; i++)
                {
                    string FunctionName = Marshal.PtrToStringAnsi((IntPtr)(ModuleBase.ToInt64() + Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + NamesRVA + i * 4))));
                    if (FunctionName.Equals(ExportName, StringComparison.OrdinalIgnoreCase))
                    {
                        Int32 FunctionOrdinal = Marshal.ReadInt16((IntPtr)(ModuleBase.ToInt64() + OrdinalsRVA + i * 2)) + OrdinalBase;
                        Int32 FunctionRVA = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + FunctionsRVA + (4 * (FunctionOrdinal - OrdinalBase))));
                        FunctionPtr = (IntPtr)((Int64)ModuleBase + FunctionRVA);
                        break;
                    }
                }
            }
            catch
            {
                // Catch parser failure
                throw new Exception(DSTR(DSTR_MOD_EXPORTS_BAD));
            }

            if (FunctionPtr == IntPtr.Zero)
            {
                // Export not found
                throw new Exception(DSTR(DSTR_EXPORT_NOT_FOUND, ExportName));
            }
            return FunctionPtr;
        }

        /// <summary>
        /// Manually map module into current process.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="rawModule">Pointer to the module base.</param>
        /// <param name="allocatedModule">Pointer to the PEINFO image.</param>
        /// <param name="PEINFO">PE_META_DATA of the module being mapped.</param>
        /// <returns>PE_MANUAL_MAP object</returns>
        private static PE_MANUAL_MAP MapModuleToMemory(IntPtr rawModule, IntPtr allocatedModule, PE_META_DATA PEINFO)
        {
            // Check module matches the process architecture
            if ((PEINFO.Is32Bit && IntPtr.Size == 8) || (!PEINFO.Is32Bit && IntPtr.Size == 4))
            {
                Marshal.FreeHGlobal(rawModule);
                throw new Exception(DSTR(DSTR_MOD_ARCHITECTURE_WRONG));
            }

            // Write PE header to memory
            UInt32 SizeOfHeaders = PEINFO.Is32Bit ? PEINFO.OptHeader32.SizeOfHeaders : PEINFO.OptHeader64.SizeOfHeaders;
            UInt32 BytesWritten;
            Native.NtWriteVirtualMemoryD((IntPtr)(-1), allocatedModule, rawModule, SizeOfHeaders);

            // Write sections to memory
            foreach (IMAGE_SECTION_HEADER ish in PEINFO.Sections)
            {
                // Calculate offsets
                IntPtr pVirtualSectionBase = (IntPtr)((UInt64)allocatedModule + ish.VirtualAddress);
                IntPtr pRawSectionBase = (IntPtr)((UInt64)rawModule + ish.PointerToRawData);

                // Write data
                BytesWritten = Native.NtWriteVirtualMemoryD((IntPtr)(-1), pVirtualSectionBase, pRawSectionBase, ish.SizeOfRawData);
                if (BytesWritten != ish.SizeOfRawData)
                {
                    throw new Exception(DSTR(DSTR_FAILED_MEMORY_WRITE));
                }
            }

            // Perform relocations
            RelocateModule(PEINFO, allocatedModule);

            // Rewrite IAT
            RewriteModuleIAT(PEINFO, allocatedModule);

            // Set memory protections
            SetModuleSectionPermissions(PEINFO, allocatedModule);

            // Free temp HGlobal
            Marshal.FreeHGlobal(rawModule);

            // Prepare return object
            PE_MANUAL_MAP ManMapObject = new PE_MANUAL_MAP
            {
                ModuleBase = allocatedModule,
                PEINFO = PEINFO
            };

            return ManMapObject;
        }

        /// <summary>
        /// Relocates a module in memory.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="PEINFO">Module meta data struct (PE.PE_META_DATA).</param>
        /// <param name="ModuleMemoryBase">Base address of the module in memory.</param>
        /// <returns>void</returns>
        private static void RelocateModule(PE_META_DATA PEINFO, IntPtr ModuleMemoryBase)
        {
            IMAGE_DATA_DIRECTORY idd = PEINFO.Is32Bit ? PEINFO.OptHeader32.BaseRelocationTable : PEINFO.OptHeader64.BaseRelocationTable;
            Int64 ImageDelta = PEINFO.Is32Bit ? (Int64)((UInt64)ModuleMemoryBase - PEINFO.OptHeader32.ImageBase) :
                                                (Int64)((UInt64)ModuleMemoryBase - PEINFO.OptHeader64.ImageBase);

            // Ptr for the base reloc table
            IntPtr pRelocTable = (IntPtr)((UInt64)ModuleMemoryBase + idd.VirtualAddress);
            Int32 nextRelocTableBlock = -1;
            // Loop reloc blocks
            while (nextRelocTableBlock != 0)
            {
                IMAGE_BASE_RELOCATION ibr;
                ibr = (IMAGE_BASE_RELOCATION)Marshal.PtrToStructure(pRelocTable, typeof(IMAGE_BASE_RELOCATION));

                Int64 RelocCount = ((ibr.SizeOfBlock - Marshal.SizeOf(ibr)) / 2);
                for (int i = 0; i < RelocCount; i++)
                {
                    // Calculate reloc entry ptr
                    IntPtr pRelocEntry = (IntPtr)((UInt64)pRelocTable + (UInt64)Marshal.SizeOf(ibr) + (UInt64)(i * 2));
                    UInt16 RelocValue = (UInt16)Marshal.ReadInt16(pRelocEntry);

                    // Parse reloc value
                    // The type should only ever be 0x0, 0x3, 0xA
                    // https://docs.microsoft.com/en-us/windows/win32/debug/pe-format#base-relocation-types
                    UInt16 RelocType = (UInt16)(RelocValue >> 12);
                    UInt16 RelocPatch = (UInt16)(RelocValue & 0xfff);

                    // Perform relocation
                    if (RelocType != 0) // IMAGE_REL_BASED_ABSOLUTE (0 -> skip reloc)
                    {
                        try
                        {
                            IntPtr pPatch = (IntPtr)((UInt64)ModuleMemoryBase + ibr.VirtualAdress + RelocPatch);
                            if (RelocType == 0x3) // IMAGE_REL_BASED_HIGHLOW (x86)
                            {
                                Int32 OriginalPtr = Marshal.ReadInt32(pPatch);
                                Marshal.WriteInt32(pPatch, (OriginalPtr + (Int32)ImageDelta));
                            }
                            else // IMAGE_REL_BASED_DIR64 (x64)
                            {
                                Int64 OriginalPtr = Marshal.ReadInt64(pPatch);
                                Marshal.WriteInt64(pPatch, (OriginalPtr + ImageDelta));
                            }
                        }
                        catch
                        {
                            throw new Exception(DSTR(DSTR_MEM_ACCESS_VIOLATION));
                        }
                    }
                }

                // Check for next block
                pRelocTable = (IntPtr)((UInt64)pRelocTable + ibr.SizeOfBlock);
                nextRelocTableBlock = Marshal.ReadInt32(pRelocTable);
            }
        }

        /// <summary>
        /// Rewrite IAT for manually mapped module.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author> (modified for external)
        /// <param name="PEINFO">Module meta data struct (PE.PE_META_DATA).</param>
        /// <param name="ModuleMemoryBase">Base address of the module in memory.</param>
        /// <returns>void</returns>
        private static void RewriteModuleIAT(PE_META_DATA PEINFO, IntPtr ModuleMemoryBase)
        {
            IMAGE_DATA_DIRECTORY idd = PEINFO.Is32Bit ? PEINFO.OptHeader32.ImportTable : PEINFO.OptHeader64.ImportTable;

            // Check if there is no import table
            if (idd.VirtualAddress == 0)
            {
                // Return so that the rest of the module mapping process may continue.
                return;
            }

            // Ptr for the base import directory
            IntPtr pImportTable = (IntPtr)((UInt64)ModuleMemoryBase + idd.VirtualAddress);

            // Get API Set mapping dictionary if on Win10+
            Native.OSVERSIONINFOEX OSVersion = new Native.OSVERSIONINFOEX();
            Native.RtlGetVersionD(ref OSVersion);
            if (OSVersion.MajorVersion < 10)
            {
                throw new Exception(DSTR(DSTR_OS_VERSION_TOO_OLD));
            }

            // Loop IID's
            int counter = 0;
            Native.IMAGE_IMPORT_DESCRIPTOR iid = new Native.IMAGE_IMPORT_DESCRIPTOR();
            iid = (Native.IMAGE_IMPORT_DESCRIPTOR)Marshal.PtrToStructure(
                (IntPtr)((UInt64)pImportTable + (uint)(Marshal.SizeOf(iid) * counter)),
                typeof(Native.IMAGE_IMPORT_DESCRIPTOR)
            );
            while (iid.Name != 0)
            {
                // Get DLL
                string DllName = string.Empty;
                try
                {
                    DllName = Marshal.PtrToStringAnsi((IntPtr)((UInt64)ModuleMemoryBase + iid.Name));
                }
                catch { }

                // Loop imports
                if (DllName == string.Empty)
                {
                    throw new Exception(DSTR(DSTR_MODULE_NAME_INVALID));
                }
                else
                {
                    // API Set DLL?
                    string dll_resolved;
                    if ((DllName.StartsWith("api-") || DllName.StartsWith("ext-")))
                    {
                        // for some reason, Lunar's resolve apiset is 1000x better than the dictionary setup done in dinvoke, so we will just use that.
                        // lmfao: https://github.com/cobbr/SharpSploit/issues/58
                        if ((dll_resolved = EnvironmentEx.ResolveAPISet(DllName)) == null)
                        {
                            throw new Exception(DSTR(DSTR_API_DLL_UNRESOLVED, DllName));
                        }
                        // Not all API set DLL's have a registered host mapping
                        DllName = dll_resolved;
                    }

                    // Check and / or load DLL
                    IntPtr hModule = GetLoadedModuleAddress(DllName);
                    if (hModule == IntPtr.Zero)
                    {
                        hModule = LoadModuleFromDisk(DllName);
                        if (hModule == IntPtr.Zero)
                        {
                            throw new Exception(DSTR(DSTR_MODULE_FILE_NOT_FOUND, DllName));
                        }
                    }

                    // Loop thunks
                    if (PEINFO.Is32Bit)
                    {
                        IMAGE_THUNK_DATA32 oft_itd;
                        for (int i = 0; true; i++)
                        {
                            oft_itd = (IMAGE_THUNK_DATA32)Marshal.PtrToStructure((IntPtr)((UInt64)ModuleMemoryBase + iid.OriginalFirstThunk + (UInt32)(i * (sizeof(UInt32)))), typeof(IMAGE_THUNK_DATA32));
                            IntPtr ft_itd = (IntPtr)((UInt64)ModuleMemoryBase + iid.FirstThunk + (UInt64)(i * (sizeof(UInt32))));
                            if (oft_itd.AddressOfData == 0)
                            {
                                break;
                            }

                            if (oft_itd.AddressOfData < 0x80000000) // !IMAGE_ORDINAL_FLAG32
                            {
                                IntPtr pImpByName = (IntPtr)((UInt64)ModuleMemoryBase + oft_itd.AddressOfData + sizeof(UInt16));
                                IntPtr pFunc;
                                pFunc = GetNativeExportAddress(hModule, Marshal.PtrToStringAnsi(pImpByName));

                                // Write ProcAddress
                                Marshal.WriteInt32(ft_itd, pFunc.ToInt32());
                            }
                            else
                            {
                                ulong fOrdinal = oft_itd.AddressOfData & 0xFFFF;
                                IntPtr pFunc;
                                pFunc = GetNativeExportAddress(hModule, (short)fOrdinal);

                                // Write ProcAddress
                                Marshal.WriteInt32(ft_itd, pFunc.ToInt32());
                            }
                        }
                    }
                    else
                    {
                        IMAGE_THUNK_DATA64 oft_itd;
                        for (int i = 0; true; i++)
                        {
                            oft_itd = (IMAGE_THUNK_DATA64)Marshal.PtrToStructure((IntPtr)((UInt64)ModuleMemoryBase + iid.OriginalFirstThunk + (UInt64)(i * (sizeof(UInt64)))), typeof(IMAGE_THUNK_DATA64));
                            IntPtr ft_itd = (IntPtr)((UInt64)ModuleMemoryBase + iid.FirstThunk + (UInt64)(i * (sizeof(UInt64))));
                            if (oft_itd.AddressOfData == 0)
                            {
                                break;
                            }

                            if (oft_itd.AddressOfData < 0x8000000000000000) // !IMAGE_ORDINAL_FLAG64
                            {
                                IntPtr pImpByName = (IntPtr)((UInt64)ModuleMemoryBase + oft_itd.AddressOfData + sizeof(UInt16));
                                IntPtr pFunc;
                                pFunc = GetNativeExportAddress(hModule, Marshal.PtrToStringAnsi(pImpByName));

                                // Write pointer
                                Marshal.WriteInt64(ft_itd, pFunc.ToInt64());
                            }
                            else
                            {
                                ulong fOrdinal = oft_itd.AddressOfData & 0xFFFF;
                                IntPtr pFunc;
                                pFunc = GetNativeExportAddress(hModule, (short)fOrdinal);

                                // Write pointer
                                Marshal.WriteInt64(ft_itd, pFunc.ToInt64());
                            }
                        }
                    }
                    counter++;
                    iid = (Native.IMAGE_IMPORT_DESCRIPTOR)Marshal.PtrToStructure(
                        (IntPtr)((UInt64)pImportTable + (uint)(Marshal.SizeOf(iid) * counter)),
                        typeof(Native.IMAGE_IMPORT_DESCRIPTOR)
                    );
                }
            }
        }

        /// <summary>
        /// Set correct module section permissions.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="PEINFO">Module meta data struct (PE.PE_META_DATA).</param>
        /// <param name="ModuleMemoryBase">Base address of the module in memory.</param>
        /// <returns>void</returns>
        public static void SetModuleSectionPermissions(PE_META_DATA PEINFO, IntPtr ModuleMemoryBase)
        {
            // Apply RO to the module header
            IntPtr BaseOfCode = PEINFO.Is32Bit ? (IntPtr)PEINFO.OptHeader32.BaseOfCode : (IntPtr)PEINFO.OptHeader64.BaseOfCode;
            Native.NtProtectVirtualMemoryD((IntPtr)(-1), ref ModuleMemoryBase, ref BaseOfCode, Native.PAGE_READONLY);

            // Apply section permissions
            foreach (IMAGE_SECTION_HEADER ish in PEINFO.Sections)
            {
                bool isRead = (ish.Characteristics & DataSectionFlags.MEM_READ) != 0;
                bool isWrite = (ish.Characteristics & DataSectionFlags.MEM_WRITE) != 0;
                bool isExecute = (ish.Characteristics & DataSectionFlags.MEM_EXECUTE) != 0;
                uint flNewProtect = 0;
                if (isRead & !isWrite & !isExecute)
                {
                    flNewProtect = Native.PAGE_READONLY;
                }
                else if (isRead & isWrite & !isExecute)
                {
                    flNewProtect = Native.PAGE_READWRITE;
                }
                else if (isRead & isWrite & isExecute)
                {
                    flNewProtect = Native.PAGE_EXECUTE_READWRITE;
                }
                else if (isRead & !isWrite & isExecute)
                {
                    flNewProtect = Native.PAGE_EXECUTE_READ;
                }
                else if (!isRead & !isWrite & isExecute)
                {
                    flNewProtect = Native.PAGE_EXECUTE;
                }
                else
                {
                    throw new Exception(DSTR(DSTR_UNK_SEC_FLAG, ish.Characteristics));
                }

                // Calculate base
                IntPtr pVirtualSectionBase = (IntPtr)((UInt64)ModuleMemoryBase + ish.VirtualAddress);
                IntPtr ProtectSize = (IntPtr)ish.VirtualSize;

                // Set protection
                Native.NtProtectVirtualMemoryD((IntPtr)(-1), ref pVirtualSectionBase, ref ProtectSize, flNewProtect);
            }
        }

        /// <summary>
        /// Given a module base address, resolve the address of a function by calling LdrGetProcedureAddress.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="ModuleBase">A pointer to the base address where the module is loaded in the current process.</param>
        /// <param name="ExportName">The name of the export to search for (e.g. "NtAlertResumeThread").</param>
        /// <returns>IntPtr for the desired function.</returns>
        public static IntPtr GetNativeExportAddress(IntPtr ModuleBase, string ExportName)
        {
            Native.ANSI_STRING aFunc = new Native.ANSI_STRING
            {
                Length = (ushort)ExportName.Length,
                MaximumLength = (ushort)(ExportName.Length + 2),
                Buffer = Marshal.StringToCoTaskMemAnsi(ExportName)
            };

            IntPtr pAFunc = Marshal.AllocHGlobal(Marshal.SizeOf(aFunc));
            Marshal.StructureToPtr(aFunc, pAFunc, true);

            IntPtr pFuncAddr = IntPtr.Zero;
            Native.LdrGetProcedureAddressD(ModuleBase, pAFunc, IntPtr.Zero, ref pFuncAddr);
            Marshal.FreeHGlobal(pAFunc);

            return pFuncAddr;
        }

        /// <summary>
        /// Given a module base address, resolve the address of a function by calling LdrGetProcedureAddress.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="ModuleBase">A pointer to the base address where the module is loaded in the current process.</param>
        /// <param name="Ordinal">The ordinal number to search for (e.g. 0x136 -> ntdll!NtCreateThreadEx).</param>
        /// <returns>IntPtr for the desired function.</returns>
        public static IntPtr GetNativeExportAddress(IntPtr ModuleBase, short Ordinal)
        {
            IntPtr pFuncAddr = IntPtr.Zero;
            IntPtr pOrd = (IntPtr)Ordinal;

            Native.LdrGetProcedureAddressD(ModuleBase, IntPtr.Zero, pOrd, ref pFuncAddr);
            return pFuncAddr;
        }
    }
}
