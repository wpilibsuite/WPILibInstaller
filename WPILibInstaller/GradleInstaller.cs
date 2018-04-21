using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        string wrapperDir;

        public GradleInstaller(CheckBox doInstall, GradleConfig jConfig, string resourceRoot, string installLoc)
        {
            this.doInstall = doInstall;
            this.jConfig = jConfig;
            this.resourceRoot = resourceRoot;

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            wrapperDir = Path.Combine(homeDir, ".gradle", "wrapper", "dists", jConfig.FolderName, jConfig.Hash, jConfig.Zip);

            ;
            //jdkDir = Path.Combine(installLoc, "gradle");
        }

        public override async Task CheckForInstall()
        {
            if (File.Exists(wrapperDir))
            {
                doInstall.Checked = false;
            }
            else
            {
                doInstall.Checked = true;
            }

            /*
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
            */
        }

        public override async Task<bool> DoInstall(ProgressBar progBar, Button displayButton, CancellationToken token)
        {


            if (doInstall.Checked)
            {
                displayButton.Text = "Installing Gradle";
                Directory.CreateDirectory(Path.GetDirectoryName(wrapperDir));
                using (var ostream = new FileStream(wrapperDir, FileMode.Create, FileAccess.Write))
                using (var istream = new FileStream(Path.Combine(resourceRoot, jConfig.Zip), FileMode.Open, FileAccess.Read))
                {
                    await istream.CopyToAsync(ostream, 4096, token);
                }
                //File.Copy()
                //await ZipTools.UnzipToDirectory(Path.Combine(resourceRoot, jConfig.Zip), jdkDir, progBar, true);
                displayButton.Text = "Finished installing Gradle";
            }

            return true;
        }
    }
}
