using Refract.UI.Core.Interfaces;
using Refract.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Refract.UI.Core.Controls
{
    public partial class CTitleBar : UserControl, IThemeableControl
    {
        public bool DisableDrag;
        public CTitleBar()
        {
            InitializeComponent();
            MouseDown += MouseDown_Drag;
            UIThemeManager.RegisterCustomThemeHandler(typeof(CTitleBar), ApplyThemeCustomType_Implementation);
            UIThemeManager.OnThemeChanged(this, ApplyThemeCustom_Implementation);
            TitleLabel.MouseDown += MouseDown_Drag;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            ParentForm?.Close();
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void MouseDown_Drag(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (ParentForm == null) return;
            if (DisableDrag) return;
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(ParentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        public void SetExitButtonVisible(bool isVisible)
        {
            ExitButton.Visible = isVisible;
        }

        private void ApplyThemeCustomType_Implementation(UIThemeInfo themeData)
        {

        }

        private void ApplyThemeCustom_Implementation(UIThemeInfo themeData)
        {
            ExitButton.BackColor = themeData.LightBackColor;
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return ExitButton;
            yield return TitleLabel;
        }
    }
}
