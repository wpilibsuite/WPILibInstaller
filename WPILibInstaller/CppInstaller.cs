using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;


namespace WPILibInstaller
{
    public class CppInstaller : Checker
    {
        CheckBox doInstall;
        CppCompilerConfig jConfig;
        string resourceRoot;
        string installLoc;
        string jdkDir;

        public CppInstaller(CheckBox doInstall, CppCompilerConfig jConfig, string resourceRoot, string installLoc)
        {
            this.doInstall = doInstall;
            this.jConfig = jConfig;
            this.resourceRoot = resourceRoot;
            this.installLoc = installLoc;
            jdkDir = Path.Combine(installLoc, "gcc");
        }

        public override async Task CheckForInstall()
        {
            var javaPath = Path.Combine(jdkDir, "bin", "arm-frc-linux-gnueabi-gcc.exe");

            var javaTask = await TaskEx.Run(() =>
            {
                try
                {
                    var gccStartInfo = new ProcessStartInfo(javaPath, "--version");
                    gccStartInfo.UseShellExecute = false;
                    gccStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    gccStartInfo.CreateNoWindow = true;
                    gccStartInfo.RedirectStandardOutput = true;
                    var gccProcess = Process.Start(gccStartInfo);
                    gccProcess.WaitForExit();
                    return gccProcess.StandardOutput.ReadToEnd();
                }
                catch (Exception)
                {
                    return null;
                }
            });
            if (javaTask == null)
            {
                doInstall.Checked = true;
            }
            else
            {
                if (javaTask.Contains(jConfig.Version))
                {
                    doInstall.Checked = false;
                }
                else
                {
                    doInstall.Checked = true;
                }
            }


        }

        public override async Task<bool> DoInstall(ProgressBar progBar, Button displayButton, CancellationToken token)
        {


            if (doInstall.Checked)
            {
                displayButton.Text = "Installing C++ Compiler";
                await ZipTools.UnzipToDirectory(Path.Combine(resourceRoot, jConfig.Zip), jdkDir, progBar, true);
                displayButton.Text = "Finished installing C++ Compiler";
            }

            return true;
        }
    }
}
