using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.PEStructures;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.EnvironmentEx;
using System.Evasion;
using static System.PEStructures.PE;
using System.Diagnostics;

namespace System
{
    public partial class ProcessEx
    {
        private PEImage __image;
        private PEImage Image
        {
            get
            {
                if(__image is null && !loadFailSilent)
                {
                    loadFailSilent = true;
                    try
                    {
                        __image = new PEImage(File.ReadAllBytes(BaseProcess.MainModule.FileName));
                    }
                    catch { }
                }
                return __image;
            }
        }

        private bool loadFailSilent = false;
        private PEActivationContext __activationContext;
        private PEActivationContext ActivationContext
        {
            get
            {
                if(__activationContext is null && !loadFailSilent)
                {
                    __activationContext = new PEActivationContext(Image.Resources.GetManifest(), this);
                }
                return __activationContext;
            }
        }

        /// <summary>
        /// Manually map module into remote process.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author> (modified for external ProcessEx)
        /// <param name="localModuleHandle">Pointer to the module base.</param>
        /// <returns>PE_MANUAL_MAP object</returns>
        private PE_MANUAL_MAP MapModuleToMemory(PointerEx localModuleHandle, byte[] moduleRaw)
        {
#if DEBUG
            DLog($"Attempting to map module of size {moduleRaw.Length}");
#endif
            // Fetch PE meta data
            PE_META_DATA PEINFO = GetPeMetaData(localModuleHandle);
            
            // Check module matches the process architecture
            if ((PEINFO.Is32Bit && IntPtr.Size == 8) || (!PEINFO.Is32Bit && IntPtr.Size == 4))
            {
                Marshal.FreeHGlobal(localModuleHandle);
                throw new InvalidOperationException(DSTR(DSTR_MOD_ARCHITECTURE_WRONG));
            }

            // Alloc PE image memory -> RW
            IntPtr RegionSize = PEINFO.Is32Bit ? (IntPtr)PEINFO.OptHeader32.SizeOfImage : (IntPtr)PEINFO.OptHeader64.SizeOfImage;
            IntPtr remoteModuleHandle = QuickAlloc(RegionSize);

#if DEBUG
            DLog($"Module memory allocated at 0x{remoteModuleHandle.ToInt64():X16}");
#endif
            return __MapModuleToMemory(localModuleHandle, remoteModuleHandle, moduleRaw, PEINFO);
        }

        /// <summary>
        /// Manually map module into remote process.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author> (modified for external ProcessEx)
        /// <param name="localModuleHandle">Pointer to the module base.</param>
        /// <param name="remoteModuleHandle">Pointer to the PEINFO image.</param>
        /// <param name="PEINFO">PE_META_DATA of the module being mapped.</param>
        /// <returns>PE_MANUAL_MAP object</returns>
        private PE_MANUAL_MAP __MapModuleToMemory(IntPtr localModuleHandle, IntPtr remoteModuleHandle, byte[] moduleRaw, PE_META_DATA PEINFO)
        {
            // Check module matches the process architecture
            if ((PEINFO.Is32Bit && IntPtr.Size == 8) || (!PEINFO.Is32Bit && IntPtr.Size == 4))
            {
                Marshal.FreeHGlobal(localModuleHandle);
                throw new InvalidOperationException(DSTR(DSTR_MOD_ARCHITECTURE_WRONG));
            }

            // Write PE header to memory
            UInt32 SizeOfHeaders = PEINFO.Is32Bit ? PEINFO.OptHeader32.SizeOfHeaders : PEINFO.OptHeader64.SizeOfHeaders;
            IntPtr RegionSize = PEINFO.Is32Bit ? (IntPtr)PEINFO.OptHeader32.SizeOfImage : (IntPtr)PEINFO.OptHeader64.SizeOfImage;

            byte[] moduleRemote = new byte[RegionSize.ToInt32()];
            moduleRaw.Take((int)SizeOfHeaders).ToArray().CopyTo(moduleRemote, 0);

#if DEBUG
            DLog($"PE Header written at 0x{remoteModuleHandle.ToInt64():X16}");
#endif

            // Write sections to memory
            foreach (IMAGE_SECTION_HEADER ish in PEINFO.Sections)
            {
                // Calculate offsets
                IntPtr pVirtualSectionBase = (IntPtr)(ish.VirtualAddress);

                moduleRaw.Skip((int)ish.PointerToRawData).Take((int)ish.SizeOfRawData).ToArray().CopyTo(moduleRemote, pVirtualSectionBase.ToInt32());
#if DEBUG
                DLog($"{new string(ish.Name).Trim().Replace('\x00'.ToString(), "")} written at 0x{pVirtualSectionBase.ToInt64():X16}, Size: 0x{ish.SizeOfRawData:X8}");
#endif
            }

            // Perform relocations
            __MMRelocateModule(PEINFO, remoteModuleHandle, moduleRaw, moduleRemote);

            // Rewrite IAT
            __MMRewriteModuleIAT(PEINFO, remoteModuleHandle, moduleRaw, moduleRemote);

            // Write the module to memory
            SetBytes(remoteModuleHandle, moduleRemote);

            // Set memory protections
            MMSetModuleSectionPermissions(PEINFO, remoteModuleHandle, moduleRaw);

            // Free temp HGlobal
            Marshal.FreeHGlobal(localModuleHandle);

            // Prepare return object
            PE_MANUAL_MAP ManMapObject = new PE_MANUAL_MAP
            {
                ModuleBase = remoteModuleHandle,
                PEINFO = PEINFO
            };
            return ManMapObject;
        }

        /// <summary>
        /// Relocates a module in remote memory.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author> (modified for external ProcessEx)
        /// <param name="PEINFO">Module meta data struct (PE.PE_META_DATA).</param>
        /// <param name="ModuleMemoryBase">Base address of the module in memory.</param>
        /// <returns>void</returns>
        private void __MMRelocateModule(PE_META_DATA PEINFO, IntPtr ModuleMemoryBase, byte[] moduleRaw, byte[] moduleRemote)
        {
            IMAGE_DATA_DIRECTORY idd = PEINFO.Is32Bit ? PEINFO.OptHeader32.BaseRelocationTable : PEINFO.OptHeader64.BaseRelocationTable;
            Int64 ImageDelta = PEINFO.Is32Bit ? (Int64)((UInt64)ModuleMemoryBase - PEINFO.OptHeader32.ImageBase) :
                                                (Int64)((UInt64)ModuleMemoryBase - PEINFO.OptHeader64.ImageBase);

            // Ptr for the base reloc table
            IntPtr pRelocTable = (IntPtr)(idd.VirtualAddress);
            Int32 nextRelocTableBlock = -1;
            // Loop reloc blocks
            while (nextRelocTableBlock != 0)
            {
                IMAGE_BASE_RELOCATION ibr = new IMAGE_BASE_RELOCATION();
                ibr = moduleRemote.Skip(pRelocTable.ToInt32()).Take(Marshal.SizeOf(typeof(IMAGE_BASE_RELOCATION))).ToArray().ToStruct<IMAGE_BASE_RELOCATION>();

                Int64 RelocCount = ((ibr.SizeOfBlock - Marshal.SizeOf(ibr)) / 2);
                for (int i = 0; i < RelocCount; i++)
                {
                    // Calculate reloc entry ptr
                    IntPtr pRelocEntry = (IntPtr)((UInt64)pRelocTable + (UInt64)Marshal.SizeOf(ibr) + (UInt64)(i * 2));
                    UInt16 RelocValue = (UInt16)BitConverter.ToUInt16(moduleRemote, pRelocEntry.ToInt32());

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
                            IntPtr pPatch = (IntPtr)(ibr.VirtualAdress + RelocPatch);
                            if (RelocType == 0x3) // IMAGE_REL_BASED_HIGHLOW (x86)
                            {
                                Int32 OriginalPtr = BitConverter.ToInt32(moduleRemote, pPatch.ToInt32());
                                BitConverter.GetBytes((OriginalPtr + (Int32)ImageDelta)).CopyTo(moduleRemote, pPatch.ToInt32());
                            }
                            else // IMAGE_REL_BASED_DIR64 (x64)
                            {
                                Int64 OriginalPtr = BitConverter.ToInt64(moduleRemote, pPatch.ToInt32());
                                BitConverter.GetBytes((OriginalPtr + ImageDelta)).CopyTo(moduleRemote, pPatch.ToInt32());
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
                nextRelocTableBlock = BitConverter.ToInt32(moduleRemote, pRelocTable.ToInt32());
            }
        }


        /// <summary>
        /// Rewrite IAT for manually mapped remote module.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author> (modified for external ProcessEx)
        /// <param name="PEINFO">Module meta data struct (PE.PE_META_DATA).</param>
        /// <param name="ModuleMemoryBase">Base address of the module in memory.</param>
        /// <returns>void</returns>
        private void __MMRewriteModuleIAT(PE_META_DATA PEINFO, IntPtr ModuleMemoryBase, byte[] moduleRaw, byte[] moduleRemote)
        {
            IMAGE_DATA_DIRECTORY idd = PEINFO.Is32Bit ? PEINFO.OptHeader32.ImportTable : PEINFO.OptHeader64.ImportTable;

            // Check if there is no import table
            if (idd.VirtualAddress == 0)
            {
                // Return so that the rest of the module mapping process may continue.
                return;
            }

            // Ptr for the base import directory
            IntPtr pImportTable = (IntPtr)(idd.VirtualAddress);

            // Get API Set mapping dictionary if on Win10+
            Native.OSVERSIONINFOEX OSVersion = new Native.OSVERSIONINFOEX();
            Native.RtlGetVersionD(ref OSVersion);
            if (OSVersion.MajorVersion < 10)
            {
                throw new Exception(DSTR(DSTR_OS_VERSION_TOO_OLD));
            }

#if DEBUG
            DLog($"Starting IAT rewrite...");
#endif

            // Loop IID's
            int counter = 0;
            Native.IMAGE_IMPORT_DESCRIPTOR iid = new Native.IMAGE_IMPORT_DESCRIPTOR();
            iid = moduleRemote.Skip(pImportTable.ToInt32() + (int)(Marshal.SizeOf(typeof(Native.IMAGE_IMPORT_DESCRIPTOR)) * counter)).Take(Marshal.SizeOf(typeof(Native.IMAGE_IMPORT_DESCRIPTOR))).ToArray().ToStruct<Native.IMAGE_IMPORT_DESCRIPTOR>();
            while (iid.Name != 0)
            {
                // Get DLL
                string DllName = string.Empty;
                try
                {
                    DllName = moduleRemote.String((int)iid.Name);
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
                        if ((dll_resolved = ResolveAPISet(DllName)) == null)
                        {
                            throw new Exception(DSTR(DSTR_API_DLL_UNRESOLVED, DllName));
                        }
                        // Not all API set DLL's have a registered host mapping
                        DllName = dll_resolved;
                    }

#if DEBUG
                    DLog($"Resolving {DllName}...");
#endif

                    // Check and / or load DLL
                    IntPtr hModule = GetLoadedModuleAddress(DllName);
                    if (hModule == IntPtr.Zero)
                    {
#if DEBUG
                        DLog($"Unresolved: {DllName}, Loading into memory...");
#endif
                        hModule = LoadAndRegisterDllRemote(DllName).BaseAddress;
                        if (hModule == IntPtr.Zero)
                        {
                            throw new Exception(DSTR(DSTR_MODULE_FILE_NOT_FOUND, DllName));
                        }
                    }

                    // Locate the module by its base address
                    ProcessModuleEx sModule = FindModuleByAddress(hModule);

#if DEBUG
                    DLog($"Resolved Module: {sModule.ModuleName}:{sModule.BaseAddress:X}, {sModule.ModulePath}");
#endif

                    // Loop thunks
                    if (PEINFO.Is32Bit)
                    {
                        IMAGE_THUNK_DATA32 oft_itd = new IMAGE_THUNK_DATA32();
                        for (int i = 0; true; i++)
                        {
                            oft_itd = moduleRemote.Skip((int)(iid.OriginalFirstThunk + (UInt32)(i * (sizeof(UInt32))))).Take(4).ToArray().ToStruct<IMAGE_THUNK_DATA32>();
                            IntPtr ft_itd = (IntPtr)((UInt64)iid.FirstThunk + (UInt64)(i * (sizeof(UInt32))));
                            if (oft_itd.AddressOfData == 0)
                            {
                                break;
                            }

                            if (oft_itd.AddressOfData < 0x80000000) // !IMAGE_ORDINAL_FLAG32
                            {
                                uint pImpByName = (uint)((uint)oft_itd.AddressOfData + sizeof(UInt16));

                                var pFunc = GetProcAddress(sModule.ModuleName, moduleRemote.String((int)pImpByName));

                                // Write ProcAddress
                                BitConverter.GetBytes((int)pFunc).CopyTo(moduleRemote, ft_itd.ToInt32());
                            }
                            else
                            {
                                ulong fOrdinal = oft_itd.AddressOfData & 0xFFFF;
                                var pFunc = GetModuleExportAddress(sModule.ModuleName, (short)fOrdinal);

                                // Write ProcAddress
                                BitConverter.GetBytes((int)pFunc).CopyTo(moduleRemote, ft_itd.ToInt32());
                            }
                        }
                    }
                    else
                    {
                        IMAGE_THUNK_DATA64 oft_itd = new IMAGE_THUNK_DATA64();
                        for (int i = 0; true; i++)
                        {
                            oft_itd = moduleRemote.Skip((int)(iid.OriginalFirstThunk + (uint)(i * sizeof(ulong)))).Take(8).ToArray().ToStruct<IMAGE_THUNK_DATA64>();
                            IntPtr ft_itd = (IntPtr)((UInt64)iid.FirstThunk + (UInt64)(i * (sizeof(UInt64))));
                            if (oft_itd.AddressOfData == 0)
                            {
                                break;
                            }

                            if (oft_itd.AddressOfData < 0x8000000000000000) // !IMAGE_ORDINAL_FLAG64
                            {
                                uint pImpByName = (uint)((uint)oft_itd.AddressOfData + sizeof(UInt16));
                                var pFunc = GetProcAddress(sModule.ModuleName, moduleRemote.String((int)pImpByName));

                                // Write pointer
                                BitConverter.GetBytes((long)pFunc).CopyTo(moduleRemote, ft_itd.ToInt32());
                            }
                            else
                            {
                                ulong fOrdinal = oft_itd.AddressOfData & 0xFFFF;
                                var pFunc = GetModuleExportAddress(sModule.ModuleName, (short)fOrdinal);

                                // Write pointer
                                BitConverter.GetBytes((long)pFunc).CopyTo(moduleRemote, ft_itd.ToInt32());
                            }
                        }
                    }
                    counter++;
                    iid = moduleRemote.Skip(pImportTable.ToInt32() + (int)(Marshal.SizeOf(typeof(Native.IMAGE_IMPORT_DESCRIPTOR)) * counter)).Take(Marshal.SizeOf(typeof(Native.IMAGE_IMPORT_DESCRIPTOR))).ToArray().ToStruct<Native.IMAGE_IMPORT_DESCRIPTOR>();
                }
            }
        }

        /// <summary>
        /// Set correct module section permissions.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author> (modified for external ProcessEx)
        /// <param name="PEINFO">Module meta data struct (PE.PE_META_DATA).</param>
        /// <param name="ModuleMemoryBase">Base address of the module in memory.</param>
        /// <returns>void</returns>
        public void MMSetModuleSectionPermissions(PE_META_DATA PEINFO, IntPtr ModuleMemoryBase, byte[] rawData)
        {
            // Apply RO to the module header
            IntPtr BaseOfCode = PEINFO.Is32Bit ? (IntPtr)PEINFO.OptHeader32.BaseOfCode : (IntPtr)PEINFO.OptHeader64.BaseOfCode;
            NativeStealth.VirtualProtectEx(Handle, ModuleMemoryBase, BaseOfCode.ToInt32(), (int)Native.PAGE_READONLY, out _);

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
                    throw new InvalidOperationException(DSTR(DSTR_UNK_SEC_FLAG, ish.Characteristics));
                }

                // Calculate base
                IntPtr pVirtualSectionBase = (IntPtr)((UInt64)ModuleMemoryBase + ish.VirtualAddress);
                IntPtr ProtectSize = (IntPtr)ish.VirtualSize;

                // Set protection
                NativeStealth.VirtualProtectEx(Handle, pVirtualSectionBase, ProtectSize.ToInt32(), (int)flNewProtect, out _);
            }
        }

        /// <summary>
        /// Locate the base address of a module by its name
        /// </summary>
        /// <param name="DLLName"></param>
        /// <returns></returns>
        public PointerEx GetLoadedModuleAddress(string DLLName)
        {
            foreach (var module in Modules)
            {
                if (module.ModulePath.ToLower().EndsWith(DLLName.ToLower()))
                {
                    return module.BaseAddress;
                }
            }
            return 0;
        }

        private string ResolveFilePath(string fileName)
        {
            // Check for .local redirection
            var dotLocalFilePath = Path.Combine(BaseProcess.MainModule.FileName, ".local", fileName);
            if (File.Exists(dotLocalFilePath))
            {
                return dotLocalFilePath;
            }

            // Check for SxS redirection
            var sxsFilePath = ActivationContext?.ProbeManifest(fileName);
            if (!(sxsFilePath is null))
            {
                return sxsFilePath;
            }

            // Search the root directory of the DLL
            //if (!(_rootDirectoryPath is null))
            //{
            //    var rootDirectoryFilePath = Path.Combine(_rootDirectoryPath, fileName);

            //    if (File.Exists(rootDirectoryFilePath))
            //    {
            //        return rootDirectoryFilePath;
            //    }
            //}

            // Search the directory from which the process was loaded
            var processDirectoryFilePath = Path.Combine(BaseProcess.MainModule.FileName, fileName);
            if (File.Exists(processDirectoryFilePath))
            {
                return processDirectoryFilePath;
            }

            // Search the System directory
            var systemDirectoryPath = GetArchitecture() == Architecture.X86 ? Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) : Environment.SystemDirectory;
            var systemDirectoryFilePath = Path.Combine(systemDirectoryPath, fileName);
            if (File.Exists(systemDirectoryFilePath))
            {
                return systemDirectoryFilePath;
            }

            // Search the Windows directory
            var windowsDirectoryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), fileName);
            if (File.Exists(windowsDirectoryFilePath))
            {
                return windowsDirectoryFilePath;
            }

            // Search the current directory
            var currentDirectoryFilePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (File.Exists(currentDirectoryFilePath))
            {
                return currentDirectoryFilePath;
            }

            // Search the directories listed in the PATH environment variable
            var path = Environment.GetEnvironmentVariable("PATH");
            return path?.Split(';').Where(Directory.Exists).Select(directory => Path.Combine(directory, fileName)).FirstOrDefault(File.Exists);
        }
    }

    #region discard
    public class MappedModuleEx_LunarUNFINISHED
    {
        public ModuleLoadOptions MLO { get; private set; }
        public Memory<byte> RawDLL { get; private set; }
        public ProcessEx HostProcess { get; private set; }
        public PointerEx ModuleHandle { get; private set; }
        public MappedModuleEx_LunarUNFINISHED(ProcessEx hostProcess, Memory<byte> moduleData, ModuleLoadOptions loadOptions)
        {
            HostProcess = hostProcess;
            RawDLL = moduleData;
            MLO = loadOptions;
        }

        public bool TryMapModule(out PointerEx hModule)
        {
            var sModule = new PEImage(RawDLL);
            hModule = NativeStealth.VirtualAllocEx(HostProcess.Handle, 0, (uint)sModule.Headers.PEHeader.SizeOfImage, Native.AllocationType.Commit | Native.AllocationType.Reserve, Native.MemoryProtection.ReadOnly);
            if (!hModule)
            {
                throw new Exception("Unable to allocate enough memory to map the module");
            }
            try
            {
                MMLoadDependencies(sModule, hModule);
            }
            catch
            {
                if (HostProcess.Handle)
                {
                    ProcessEx.VirtualFreeEx(HostProcess.Handle, hModule, (uint)sModule.Headers.PEHeader.SizeOfImage, (int)FreeType.Release);
                }
                throw;
            }
            ModuleHandle = hModule;
            return true;
        }

        private void MMLoadDependencies(PEImage sModule, PointerEx hModule)
        {
            var activationContext = new PEActivationContext(sModule.Resources.GetManifest(), HostProcess);
            
            foreach(var dependency in sModule.Imports.GetImportDescriptors())
            {
                //var dependencyFilePath = ResolveFilePath(activationContext, _processContext.ResolveModuleName(dependency.Name));
            }
        }

        private string ResolveFilePath(PEActivationContext activationContext, string fileName)
        {
            // Check for .local redirection
            var dotLocalFilePath = Path.Combine(HostProcess.BaseProcess.MainModule.FileName, ".local", fileName);
            if (File.Exists(dotLocalFilePath))
            {
                return dotLocalFilePath;
            }

            // Check for SxS redirection
            var sxsFilePath = activationContext.ProbeManifest(fileName);
            if (!(sxsFilePath is null))
            {
                return sxsFilePath;
            }

            // Search the root directory of the DLL
            //if (!(_rootDirectoryPath is null))
            //{
            //    var rootDirectoryFilePath = Path.Combine(_rootDirectoryPath, fileName);

            //    if (File.Exists(rootDirectoryFilePath))
            //    {
            //        return rootDirectoryFilePath;
            //    }
            //}

            // Search the directory from which the process was loaded
            var processDirectoryFilePath = Path.Combine(HostProcess.BaseProcess.MainModule.FileName, fileName);
            if (File.Exists(processDirectoryFilePath))
            {
                return processDirectoryFilePath;
            }

            // Search the System directory
            var systemDirectoryPath = HostProcess.GetArchitecture() == Architecture.X86 ? Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) : Environment.SystemDirectory;
            var systemDirectoryFilePath = Path.Combine(systemDirectoryPath, fileName);
            if (File.Exists(systemDirectoryFilePath))
            {
                return systemDirectoryFilePath;
            }

            // Search the Windows directory
            var windowsDirectoryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), fileName);
            if (File.Exists(windowsDirectoryFilePath))
            {
                return windowsDirectoryFilePath;
            }

            // Search the current directory
            var currentDirectoryFilePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (File.Exists(currentDirectoryFilePath))
            {
                return currentDirectoryFilePath;
            }

            // Search the directories listed in the PATH environment variable
            var path = Environment.GetEnvironmentVariable("PATH");
            return path?.Split(';').Where(Directory.Exists).Select(directory => Path.Combine(directory, fileName)).FirstOrDefault(File.Exists);
        }
    }
    #endregion
}
