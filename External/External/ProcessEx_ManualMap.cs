using System;
using System.Collections.Generic;
using System.Linq;
using System.PEStructures;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public partial class ProcessEx
    {
        private void MMLoadDependencies(PEImage sModule, PointerEx hModule)
        {
            var activationContext = new PEActivationContext(sModule.Resources.GetManifest(), BaseProcess);
        }
    }
}
