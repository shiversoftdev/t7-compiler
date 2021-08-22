using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.PEStructures;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.EnvironmentEx;

namespace System
{
    public class ProcessModuleEx
    {
        private Dictionary<string, ProcessModuleExportEx> ModuleExportsByName = new Dictionary<string, ProcessModuleExportEx>();
        private Dictionary<int, ProcessModuleExportEx> ModuleExportsByOrdinal = new Dictionary<int, ProcessModuleExportEx>();
        private bool HasLoadedExports = false;
        public ProcessModule BaseModule { get; private set; }
        private PointerEx __baseaddress;
        private string __modulepath;
        public PointerEx BaseAddress
        {
            get
            {
                if (BaseModule is null)
                {
                    return __baseaddress;
                }
                return BaseModule.BaseAddress;
            }
        }
        public string ModulePath
        {
            get
            {
                if (BaseModule is null)
                {
                    return __modulepath;
                }
                return BaseModule.FileName;
            }
        }

        private string __modulename;
        public string ModuleName
        {
            get
            {
                if(BaseModule is null)
                {
                    return __modulename ?? (__modulename = Path.GetFileName(ModulePath).ToLowerInvariant());
                }
                return BaseModule.ModuleName;
            }
        }

        internal ProcessModuleEx(ProcessModule module)
        {
            BaseModule = module;
        }

        internal ProcessModuleEx(PointerEx baseAddress, string modulePath)
        {
            __baseaddress = baseAddress;
            __modulepath = modulePath;
        }

        /// <summary>
        /// Retrieve the absolute address of an export for this module, by name.
        /// </summary>
        /// <param name="exportName"></param>
        /// <returns></returns>
        internal ProcessModuleExportEx GetExportedFunction(string exportName)
        {
            // If we have this information cached, return cached info
            if (!HasLoadedExports)
            {
                CacheExports();
            }
            if(!ModuleExportsByName.ContainsKey(exportName))
            {
                throw new Exception(DSTR(DSTR_MODULE_EXPORT_NOT_FOUND, ModuleName, exportName));
            }
            return ModuleExportsByName[exportName];
        }

        internal ProcessModuleExportEx GetExportedFunction(int exportOrdinal)
        {
            if (!HasLoadedExports)
            {
                CacheExports();
            }
            return ModuleExportsByOrdinal[exportOrdinal];
        }

        private void CacheExports()
        {
            var TempFileBuffer = File.ReadAllBytes(ModulePath);

            // Parse the PE structure
            PEImage sModule = new PEImage(TempFileBuffer);
            foreach (var export in sModule.Exports.Get())
            {
                ProcessModuleExportEx exportEx = new ProcessModuleExportEx(export.Name, export.RelativeAddress + BaseAddress, export.Ordinal, export.ForwarderString);
                if (exportEx.Name != null)
                {
                    ModuleExportsByName[export.Name] = exportEx;
                }
                ModuleExportsByOrdinal[export.Ordinal] = exportEx;
            }
            HasLoadedExports = true;
        }

        #region overrides
        public static implicit operator ProcessModule(ProcessModuleEx pmx)
        {
            return pmx.BaseModule;
        }

        public static implicit operator ProcessModuleEx(ProcessModule pm)
        {
            return new ProcessModuleEx(pm);
        }

        public PointerEx this[PointerEx offset]
        {
            get
            {
                return offset + BaseAddress;
            }
        }
        #endregion
    }

    /// <summary>
    /// Data only class meant to hold a module's export's data.
    /// </summary>
    internal class ProcessModuleExportEx
    { 
        public string Name { get; private set; }
        public PointerEx AbsoluteAddress { get; private set; }
        public int Ordinal { get; private set; }
        public string Forwarder { get; private set; }
        public ProcessModuleExportEx(string name, PointerEx absoluteAddress, int ordinal, string forwarderString)
        {
            Ordinal = ordinal;
            AbsoluteAddress = absoluteAddress;
            Name = name;
            Forwarder = forwarderString;
        }
    }
}
