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
    public class GradleInstaller : Checker
    {
        CheckBox doInstall;
        GradleConfig jConfig;
        string resourceRoot;
        string installLoc;
        string jdkDir;

        public GradleInstaller(CheckBox doInstall, GradleConfig jConfig, string resourceRoot, string installLoc)
        {
            this.doInstall = doInstall;
            this.jConfig = jConfig;
            this.resourceRoot = resourceRoot;
            this.installLoc = installLoc;
            jdkDir = Path.Combine(installLoc, "gradle");
        }

        public override async Task CheckForInstall()
        {
            var javaPath = Path.Combine(jdkDir, "bin", "gradle.bat");

            var javaTask = await TaskEx.Run(() =>
            {
                try
                {
                    var javaStartInfo = new ProcessStartInfo(javaPath, "-v");
                    javaStartInfo.UseShellExecute = false;
                    javaStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    javaStartInfo.CreateNoWindow = true;
                    javaStartInfo.RedirectStandardOutput = true;
                    var javaProcess = Process.Start(javaStartInfo);
                    javaProcess.WaitForExit();
                    return javaProcess.StandardOutput.ReadToEnd();
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
                displayButton.Text = "Installing Gradle";
                await ZipTools.UnzipToDirectory(Path.Combine(resourceRoot, jConfig.Zip), jdkDir, progBar, true);
                displayButton.Text = "Finished installing Gradle";
            }

            return true;
        }
    }
}
