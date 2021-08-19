using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public class ProcessModuleEx
    {
        public ProcessModule BaseModule { get; private set; }
        public ProcessModuleEx(ProcessModule module)
        {
            BaseModule = module;
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
                return offset + BaseModule.BaseAddress;
            }
        }
        #endregion
    }
}
