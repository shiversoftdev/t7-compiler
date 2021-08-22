using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Refract.UI.Core.Interfaces
{
    internal interface IThemeableControl
    {
        /// <summary>
        /// Get all controls to register with the theme manager, except the current control, which is always registered
        /// </summary>
        /// <returns></returns>
        IEnumerable<Control> GetThemedControls();
    }
}
