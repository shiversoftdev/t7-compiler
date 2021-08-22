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

namespace SMC.UI.Core.Controls
{
    public partial class CComboDialog : Form, IThemeableControl
    {
        public object SelectedValue { get; private set; }
        public int SelectedIndex { get; private set; }
        public CComboDialog(string title, object[] selectables, int defaultIndex = 0)
        {
            InitializeComponent();
            UIThemeManager.OnThemeChanged(this, OnThemeChanged_Implementation);
            this.SetThemeAware();
            MaximizeBox = true;
            MinimizeBox = true;
            Text = title;
            InnerForm.TitleBarTitle = title;
            cComboBox1.Items.Clear();
            cComboBox1.Items.AddRange(selectables);
            if(defaultIndex > -1 && defaultIndex < selectables.Length)
            {
                cComboBox1.SelectedIndex = defaultIndex;
            }
        }

        private void OnThemeChanged_Implementation(UIThemeInfo themeData)
        {
            return;
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return cComboBox1;
            yield return AcceptButton;
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndex = cComboBox1.SelectedIndex;
            if(SelectedIndex >= 0 && SelectedIndex < cComboBox1.Items.Count)
            {
                SelectedValue = cComboBox1.Items[SelectedIndex];
            }
            else
            {
                SelectedValue = null;
            }
        }

        private void InnerForm_Load(object sender, EventArgs e)
        {

        }
    }
}
