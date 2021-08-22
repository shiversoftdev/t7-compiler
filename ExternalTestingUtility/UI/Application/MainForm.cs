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
using System.Diagnostics;
using SMC.UI.Core.Controls;

namespace t7c_installer
{
    public partial class MainForm : Form, IThemeableControl
    {
        public MainForm()
        {
            InitializeComponent();
            UIThemeManager.OnThemeChanged(this, OnThemeChanged_Implementation);
            this.SetThemeAware();
            MaximizeBox = true;
            MinimizeBox = true;
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return UpdateButton;
            yield return ConvertProj;
            yield return InstallVSCExt;
            yield return JoinDiscord;
            yield return CreateDefaultProject;
        }

        private void OnThemeChanged_Implementation(UIThemeInfo currentTheme)
        {
        }

        private void RPCTest1_Click(object sender, EventArgs e)
        {

        }

        private void RPCExample2_Click(object sender, EventArgs e)
        {

        }

        private void RPCExample3_Click(object sender, EventArgs e)
        {
        }

        private void ExampleRPC4_Click(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void JoinDiscord_Click(object sender, EventArgs e)
        {
            Process.Start("https://gsc.dev/s/discord");
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            Program.InstallUpdate();
            CErrorDialog.Show("Success!", "Compiler installed successfully", true);
        }

        private void InstallVSCExt_Click(object sender, EventArgs e)
        {
            Program.UpdateVSCExtension();
            CErrorDialog.Show("Success!", "Extension installed successfully", true);
        }

        private void CreateDefaultProject_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;
            fbd.Description = "Select a folder to copy the default project to";
            if (fbd.ShowDialog() != DialogResult.OK) return;
            Program.CopyDefaultProject(fbd.SelectedPath);
            Process.Start(fbd.SelectedPath);
            CErrorDialog.Show("Success!", "Project installed successfully", true);
        }

        private void ConvertProj_Click(object sender, EventArgs e)
        {
            Visible = false;
            new ImportDialog().ShowDialog();
            Visible = true;
        }
    }
}
