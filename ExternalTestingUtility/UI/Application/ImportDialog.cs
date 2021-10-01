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
using System.IO;

namespace t7c_installer
{
    public partial class ImportDialog : Form, IThemeableControl
    {
        private string ImportFolderPath;
        private string OutputFolderPath;
        public ImportDialog()
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
            yield return SelectImportBtn;
            yield return ImportLabel;
            yield return OutputLabel;
            yield return OutputBtn;
            yield return StartImportButton;
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

        private void SelectImportButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = false;
            fbd.Description = "Select a project to import";
            if (fbd.ShowDialog() != DialogResult.OK) return;
            if(!Directory.Exists(fbd.SelectedPath))
            {
                CErrorDialog.Show("Selection failed!", "Cannot import a project which does not exist", true);
                return;
            }
            ImportFolderPath = fbd.SelectedPath;
            ImportLabel.Text = ImportFolderPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "Error";
        }

        private void OutputBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;
            fbd.Description = "Select an output folder";
            if (fbd.ShowDialog() != DialogResult.OK) return;
            if (Directory.Exists(fbd.SelectedPath) && !IsDirectoryEmpty(fbd.SelectedPath))
            {
                MessageBox.Show("You cannot select this folder for output. The folder you select must be empty.", "DANGER: Folder has contents", MessageBoxButtons.OKCancel);
                return;
            }
            OutputFolderPath = fbd.SelectedPath;
            OutputLabel.Text = OutputFolderPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "Error";
        }

        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        private void StartImportButton_Click(object sender, EventArgs e)
        {
            if (ImportFolderPath is null || OutputFolderPath is null)
            {
                CErrorDialog.Show("Conversion Failed!", "You must specify both a folder to import and a folder to output your converted project.", true);
                return;
            }

            if(ImportFolderPath.Contains(OutputFolderPath) || OutputFolderPath.Contains(ImportFolderPath))
            {
                CErrorDialog.Show("Conversion Failed!", "Your import and output folders must be in different directories. You either have your import folder inside your output folder, or your output folder inside your import folder.", true);
                return;
            }

            // 1. Clear output folder
            if (Directory.Exists(OutputFolderPath))
            {
                Directory.Delete(OutputFolderPath, true);
            }

            if(File.Exists(Path.Combine(ImportFolderPath, "gsc.conf")))
            {
                Program.DirectoryCopy(ImportFolderPath, OutputFolderPath, true);
                CErrorDialog.Show("Success!", "Your project was migrated successfully!", true);
                return;
            }
            
            // 2. Install a default project in that directory
            Program.CopyDefaultProject(OutputFolderPath, "T7", true);

            // 3. Clear out the scripts folder
            string scripts = Path.Combine(OutputFolderPath, "scripts");
            Directory.Delete(scripts, true);

            // 4. Copy all the project files from the imported project into scripts
            Program.DirectoryCopy(ImportFolderPath, scripts, true);

            // 5. Fix up all the references to EnableOnlineMatch
            foreach (var file in Directory.GetFiles(scripts, "*.gsc", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(file);
                string lower = source.ToLower();
                int index;
                while((index = lower.IndexOf("enableonlinematch")) > -1)
                {
                    // this is probably so inefficient but whatever lmfao
                    source = new string(source.Take(index).ToArray()) + "getplayers" + new string(source.Skip(index + "enableonlinematch".Length).ToArray());
                    lower = source.ToLower();
                }
                File.WriteAllText(file, source);
            }

            // 6. Try to create a gsc.conf from a config.il, otherwise, just create a default gsc.conf
            var config = Directory.GetFiles(scripts, "config.il", SearchOption.AllDirectories).FirstOrDefault();
            HashSet<string> symbols = new HashSet<string>();
            symbols.Add("BO3");
            symbols.Add("SERIOUS");
            if (config != null)
            {
                string configData = File.ReadAllText(config);
                int index;
                if((index = configData.IndexOf("<Mode>")) > -1)
                {
                    symbols.Add(new string(configData.Skip(index + "<Mode>".Length).TakeWhile((c) => { return c != '<'; }).ToArray()).ToUpper());
                }
                if ((index = configData.IndexOf("<Symbols>")) > -1)
                {
                    foreach(var symbol in new string(configData.Skip(index + "<Symbols>".Length).TakeWhile((c) => { return c != '<'; }).ToArray()).ToUpper().Split(';'))
                    {
                        symbols.Add(symbol);
                    }
                }
                File.Delete(config);
            }

            File.WriteAllText(Path.Combine(OutputFolderPath, "gsc.conf"), $"symbols={string.Join(",", symbols.ToArray())}");
            CErrorDialog.Show("Success!", "Your project was migrated successfully!", true);
        }
    }
}
