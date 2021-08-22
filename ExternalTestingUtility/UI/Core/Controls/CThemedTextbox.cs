using Refract.UI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMC.UI.Core.Controls
{
    public partial class CThemedTextbox : TextBox
    {
        const int WM_NCPAINT = 0x85;
        const uint RDW_INVALIDATE = 0x1;
        const uint RDW_IUPDATENOW = 0x100;
        const uint RDW_FRAME = 0x400;
        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprc, IntPtr hrgn, uint flags);

        private Color __borderColor = Color.Red;
        public Color BorderColor
        {
            get { return __borderColor; }
            set
            {
                __borderColor = value;
                RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero,
                    RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
            }
        }
        protected override void WndProc(ref Message m)
        {

            if (m.Msg == WM_NCPAINT && BorderColor != Color.Transparent &&
                BorderStyle == BorderStyle.Fixed3D)
            {
                var hdc = GetWindowDC(this.Handle);
                using (var g = Graphics.FromHdcInternal(hdc))
                {
                    using (var p = new Pen(BorderColor))
                    {
                        g.DrawRectangle(p, new Rectangle(0, 0, Width - 1, Height - 1));
                    }

                    using (var p = new Pen(BackColor))
                    {
                        g.DrawRectangle(p, new Rectangle(1, 1, Width - 3, Height - 3));
                    }
                }
                
                ReleaseDC(this.Handle, hdc);
            }
            else
            {
                base.WndProc(ref m);
            }
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero,
                   RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
        }

        public CThemedTextbox()
        {
            InitializeComponent();
        }
    }
}
