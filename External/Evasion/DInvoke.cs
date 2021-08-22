using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.PEStructures.PE;
using static System.EnvironmentEx;

namespace System.Evasion
{
    /// <summary>
    /// Dynamic invocation of dll functions in this process context (mostly pasted from dinvoke library)
    /// </summary>
    public static class DInvoke
    {
        // NOTE: For security, it is better to use CallMappedDLLModuleExport. The number of references to this type of d/invoke should be minimized
        /// <summary>
        /// Dynamically invoke an arbitrary function from a DLL, providing its name, function prototype, and arguments.
        /// </summary>
        /// <author>The Wover (@TheRealWover)</author>
        /// <param name="DLLName">Name of the DLL.</param>
        /// <param name="FunctionName">Name of the function.</param>
        /// <param name="FunctionDelegateType">Prototype for the function, represented as a Delegate object.</param>
        /// <param name="Parameters">Parameters to pass to the function. Can be modified if function uses call by reference.</param>
        /// <returns>Object returned by the function. Must be unmarshalled by the caller.</returns>
        public static object DynamicAPIInvoke(string DLLName, string FunctionName, Type FunctionDelegateType, ref object[] Parameters)
        {
            IntPtr pFunction = ModuleMapper.GetLibraryAddress(DLLName, FunctionName);
            return DynamicFunctionInvoke(pFunction, FunctionDelegateType, ref Parameters);
        }

        /// <summary>
        /// Dynamically invokes an arbitrary function from a pointer. Useful for manually mapped modules or loading/invoking unmanaged code from memory.
        /// </summary>
        /// <author>The Wover (@TheRealWover)</author>
        /// <param name="FunctionPointer">A pointer to the unmanaged function.</param>
        /// <param name="FunctionDelegateType">Prototype for the function, represented as a Delegate object.</param>
        /// <param name="Parameters">Arbitrary set of parameters to pass to the function. Can be modified if function uses call by reference.</param>
        /// <returns>Object returned by the function. Must be unmarshalled by the caller.</returns>
        private static object DynamicFunctionInvoke(IntPtr FunctionPointer, Type FunctionDelegateType, ref object[] Parameters)
        {
            Delegate funcDelegate = Marshal.GetDelegateForFunctionPointer(FunctionPointer, FunctionDelegateType);
            return funcDelegate.DynamicInvoke(Parameters);
        }

        /// <summary>
        /// Call a manually mapped DLL by Export.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="PEINFO">Module meta data struct (PE.PE_META_DATA).</param>
        /// <param name="ModuleMemoryBase">Base address of the module in memory.</param>
        /// <param name="ExportName">The name of the export to search for (e.g. "NtAlertResumeThread").</param>
        /// <param name="FunctionDelegateType">Prototype for the function, represented as a Delegate object.</param>
        /// <param name="Parameters">Arbitrary set of parameters to pass to the function. Can be modified if function uses call by reference.</param>
        /// <param name="CallEntry">Specify whether to invoke the module's entry point.</param>
        /// <returns>void</returns>
        public static object CallMappedDLLModuleExport(PE_META_DATA PEINFO, IntPtr ModuleMemoryBase, string ExportName, Type FunctionDelegateType, ref object[] Parameters, bool CallEntry = true)
        {
            // Call entry point if user has specified
            if (CallEntry)
            {
                CallMappedDLLModule(PEINFO, ModuleMemoryBase);
            }

            // Get export pointer
            IntPtr pFunc = ModuleMapper.GetExportAddress(ModuleMemoryBase, ExportName);

            // Call export
            return DynamicFunctionInvoke(pFunc, FunctionDelegateType, ref Parameters);
        }

        /// <summary>
        /// Call a manually mapped DLL by DllMain -> DLL_PROCESS_ATTACH.
        /// </summary>
        /// <author>Ruben Boonen (@FuzzySec)</author>
        /// <param name="PEINFO">Module meta data struct (PE.PE_META_DATA).</param>
        /// <param name="ModuleMemoryBase">Base address of the module in memory.</param>
        /// <returns>void</returns>
        private static void CallMappedDLLModule(PE_META_DATA PEINFO, IntPtr ModuleMemoryBase)
        {
            IntPtr lpEntryPoint = PEINFO.Is32Bit ? (IntPtr)((UInt64)ModuleMemoryBase + PEINFO.OptHeader32.AddressOfEntryPoint) :
                                                   (IntPtr)((UInt64)ModuleMemoryBase + PEINFO.OptHeader64.AddressOfEntryPoint);

            DllMain fDllMain = (DllMain)Marshal.GetDelegateForFunctionPointer(lpEntryPoint, typeof(DllMain));
            bool CallRes = fDllMain(ModuleMemoryBase, DLL_PROCESS_ATTACH, IntPtr.Zero);
            if (!CallRes)
            {
                throw new Exception(DSTR(DSTR_DINVOKE_MAIN_FAILED));
            }
        }

        /// <summary>
        /// Call a manually mapped DLL by Export. Will load the dll in question if not already loaded, and execute an exported function from it.
        /// </summary>
        public static object ManualInvoke(string dllPath, string ExportName, Type FunctionDelegateType, ref object[] Parameters, bool CallEntry = true)
        {
            PE_MANUAL_MAP mapData = ModuleMapper.MapModuleToMemory(dllPath);
            return CallMappedDLLModuleExport(mapData.PEINFO, mapData.ModuleBase, ExportName, FunctionDelegateType, ref Parameters, CallEntry);
        }
    }
}
