using SMC.UI.Core.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace t7c_installer
{
    static class Program
    {
#if DEBUG
        private const bool NoErrorHandling = false;
#endif
        private static string PackageURL = "https://gsc.dev/t7c_package";
        internal static bool IsUpdating = false;
        private const string InstallRoot = @"C:\";
        private static string UpdateTempFilename => Path.Combine(Path.GetTempPath(), "t7c_update.zip");
        private static string UpdateTempDirname => Path.Combine(Path.GetTempPath(), "t7c_temp");
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!Debugger.IsAttached
#if DEBUG
                && !NoErrorHandling
#endif
                )

            {
                Application.ThreadException += new ThreadExceptionEventHandler(HandleUIThreadExceptions);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleCurrentDomainExceptions);
            }

            try
            {
                if (args.Length > 0)
                {
                    switch (args[0].Trim().ToLower())
                    {
                        case "--install_silent":
                            InstallUpdate();
                            CErrorDialog.Show("Compiler Updated!", $"Your t7 compiler installation was just updated. You may proceed with your compilation action.", true);
                            return;

                        case "--deploy":
                            string compilerDirectory = args[1];
                            string defaultProjectDirectory = args[2];
                            string solutionDirectory = args[3];
                            DeployCompiler(compilerDirectory, defaultProjectDirectory, solutionDirectory);
                            return;
                    }
                }
            }
            catch(Exception e)
            {
                if(IsUpdating)
                {
                    CErrorDialog.Show("Error Updating!", $"Failed to install: {e}", true);
                }
                else
                {
                    Console.WriteLine(e.ToString());
                }
                Environment.Exit(1);
            }
            Application.Run(new MainForm());
        }

        static void DeployCompiler(string compilerDirectory, string defaultProjectDirectory, string solutionDirectory)
        {
            string installer = Assembly.GetEntryAssembly().Location;

            // clear __depot
            string depot = Path.Combine(solutionDirectory, "__depot");
            string build = Path.Combine(depot, "build");
            if (Directory.Exists(depot)) Directory.Delete(depot, true);

            // create directories for build
            Directory.CreateDirectory(depot);
            Directory.CreateDirectory(build);

            // pack compiler into __depot/build/t7compiler
            string compilerTarget = Path.Combine(build, "t7compiler");
            Directory.CreateDirectory(compilerTarget);
            foreach(var file in Directory.GetFiles(compilerDirectory))
            {
                File.Copy(file, Path.Combine(compilerTarget, Path.GetFileName(file)), true);
            }

            // copy this utility to the output folder for reuse later
            File.Copy(installer, Path.Combine(compilerTarget, Path.GetFileName(installer)));

            // pack default project into __depot/build/defaultproject
            string dprojTarget = Path.Combine(build, "defaultproject");
            Directory.CreateDirectory(dprojTarget);
            DirectoryCopy(defaultProjectDirectory, dprojTarget, true);

            // pack vsix into __depot/build/
            var files = Directory.GetFiles(solutionDirectory, "*.vsix");
            if(files.Length > 0)
            {
                File.Copy(files[files.Length - 1], Path.Combine(build, Path.GetFileName(files[files.Length - 1])));
            }

            ZipFile.CreateFromDirectory(build, Path.Combine(depot, "update.zip"));
            File.Copy(installer, Path.Combine(depot, Path.GetFileName(installer)));
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        public static void FetchUpdateContents()
        {
            if (File.Exists(UpdateTempFilename)) File.Delete(UpdateTempFilename);
            if (Directory.Exists(UpdateTempDirname)) Directory.Delete(UpdateTempDirname, true);
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(PackageURL, UpdateTempFilename);
            }
            ZipFile.ExtractToDirectory(UpdateTempFilename, UpdateTempDirname);
        }

        public static void InstallUpdate()
        {
            if (IsUpdating) return;
            IsUpdating = true;
            // cache update contents
            FetchUpdateContents();

            // kill all running instances of the compiler
            foreach (var proc in Process.GetProcessesByName("debugcompiler"))
            {
                proc.Kill();
                System.Threading.Thread.Sleep(100);
            }

            // purge old installation
            if(Directory.Exists(Path.Combine(InstallRoot, "t7compiler")))
            {
                Directory.Delete(Path.Combine(InstallRoot, "t7compiler"), true);
            }

            // copy new installation
            DirectoryCopy(Path.Combine(UpdateTempDirname, "t7compiler"), Path.Combine(InstallRoot, "t7compiler"), true);

            // copy default project
            DirectoryCopy(Path.Combine(UpdateTempDirname, "defaultproject"), Path.Combine(InstallRoot, "t7compiler", "defaultproject"), true);

            // Install the vsc extension
            NoExcept(InstallVSCExtensionsCached);
            IsUpdating = false;
        }

        public static void UpdateVSCExtension()
        {
            if (IsUpdating) return;
            IsUpdating = true;

            // cache update contents
            FetchUpdateContents();

            // Install the vsc extension
            InstallVSCExtensionsCached();
            IsUpdating = false;
        }

        public static void CopyDefaultProject(string path, string gameExt, bool noAppend = false)
        {
            if(!noAppend)
            {
                path = Path.Combine(path, "Default Project");
            }
            if (Directory.Exists(Path.Combine(InstallRoot, "t7compiler", "defaultproject", gameExt)))
            {
                // copy default project
                DirectoryCopy(Path.Combine(InstallRoot, "t7compiler", "defaultproject", gameExt), path, true);
                return;
            }
            if (Directory.Exists(Path.Combine(UpdateTempDirname, "defaultproject", gameExt)))
            {
                // restore default project
                DirectoryCopy(Path.Combine(UpdateTempDirname, "defaultproject"), Path.Combine(InstallRoot, "t7compiler", "defaultproject"), true);

                // copy default project
                DirectoryCopy(Path.Combine(UpdateTempDirname, "defaultproject", gameExt), path, true);
                return;
            }

            if (IsUpdating) return;
            IsUpdating = true;

            // cache update contents
            FetchUpdateContents();
            IsUpdating = false;

            // try again
            CopyDefaultProject(path, gameExt, noAppend);
        }

        internal static void NoExcept(Action a)
        {
            try
            {
                a();
            }
            catch { }
        }

        private static void InstallVSCExtensionsCached()
        {
            foreach (var file in Directory.GetFiles(UpdateTempDirname, "*.vsix"))
            {
                InstallExtension(file);
            }
        }

        private static void InstallExtension(string path)
        {
            using (Process proc = Process.Start(new ProcessStartInfo()
            {
                FileName = "code",
                Arguments = $"--install-extension \"{path}\"",
                UseShellExecute = true,
            }))
            {
                proc.WaitForExit();
            }
        }

        private static void HandleUIThreadExceptions(object sender, ThreadExceptionEventArgs args)
        {
            try
            {
                CErrorDialog.Show("Error", args.Exception.Message, true);
                IsUpdating = false;
                return;
            }
            catch
            {
                try
                {
                    MessageBox.Show("Fatal internal exception...", "Error", MessageBoxButtons.OK);
                }
                finally
                {
                    Application.Exit();
                }
            }
        }

        private static void HandleCurrentDomainExceptions(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                CErrorDialog.Show("Error", ((Exception)args.ExceptionObject).Message, true);
                IsUpdating = false;
                return;
            }
            catch
            {
                try
                {
                    MessageBox.Show("Fatal internal exception...", "Error", MessageBoxButtons.OK);
                }
                finally
                {
                    Application.Exit();
                }
            }
        }
    }
}
