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
    public class JavaInstaller : Checker
    {
        CheckBox doInstall;
        JavaConfig jConfig;
        string resourceRoot;
        string installLoc;
        string jdkDir;

        public JavaInstaller(CheckBox doInstall, JavaConfig jConfig, string resourceRoot, string installLoc)
        {
            this.doInstall = doInstall;
            this.jConfig = jConfig;
            this.resourceRoot = resourceRoot;
            this.installLoc = installLoc;
            jdkDir = Path.Combine(installLoc, "jdk");
        }

        public override async Task CheckForInstall()
        {
            var javaPath = Path.Combine(jdkDir, "bin", "java");

            var javaTask = await TaskEx.Run(() =>
            {
                try
                {
                    var javaStartInfo = new ProcessStartInfo(javaPath, "-version");
                    javaStartInfo.UseShellExecute = false;
                    javaStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    javaStartInfo.CreateNoWindow = true;
                    javaStartInfo.RedirectStandardError = true;
                    var javaProcess = Process.Start(javaStartInfo);
                    javaProcess.WaitForExit();
                    return javaProcess.StandardError.ReadToEnd();
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
                displayButton.Text = "Installing Java";
                await ZipTools.UnzipToDirectory(Path.Combine(resourceRoot, jConfig.Zip), jdkDir, progBar);
                displayButton.Text = "Finished installing java";
            }

            return true;
        }
    }
}
