using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WPILibInstaller
{
    public class VsCodeExtensionInstallers : Checker
    {
        CheckBox doInstall;
        List<string> config;
        string resourceRoot;

        List<string> toInstallFiles = new List<string>();

        public VsCodeExtensionInstallers(CheckBox doInstall, List<string> config, string resourceRoot)
        {
            this.doInstall = doInstall;
            this.config = config;
            this.resourceRoot = resourceRoot;
        }

        public override async Task CheckForInstall()
        {
            var codeTask = await TaskEx.Run(() =>
            {
                try
                {
                    var startInfo = new ProcessStartInfo(Path.Combine(resourceRoot, "findcodeextensions.bat"));
                    startInfo.UseShellExecute = false;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                    startInfo.RedirectStandardOutput = true;
                    var proc = Process.Start(startInfo);
                    proc.WaitForExit();
                    return proc.StandardOutput.ReadToEnd();
                }
                catch (Exception e)
                {
                    return null;
                }
            });

            if (codeTask == null)
            {
                doInstall.Checked = true;
                return;
            }

            var installedExtensions = codeTask.Replace("\r", "")
                                                   .Split('\n')
                                                   .Where(x => !string.IsNullOrWhiteSpace(x))
                                                   .Select(x =>
                                                   {
                                                       var s = x.Split(new char[] { '.' }, 2);
                                                       var v = s[1].Split('@');
                                                       return (publisher: s[0], name: v[0], version: v[1]);
                                                   }).ToArray();

            foreach (var file in config)
            {
                using (FileStream fs = new FileStream(Path.Combine(resourceRoot, file), FileMode.Open))
                using (ZipFile zf = new ZipFile(fs))
                {
                    zf.IsStreamOwner = false;

                    var entry = zf.GetEntry("extension/package.json");

                    Stream zipStream = zf.GetInputStream(entry);

                    using (StreamReader reader = new StreamReader(zipStream))
                    {
                        var str = await reader.ReadToEndAsync();
                        dynamic json = JsonConvert.DeserializeObject(str);
                        string version = json.version;
                        string name = json.name;
                        string publisher = json.publisher;

                        bool found = false;
                        foreach(var i in installedExtensions)
                        {
                            if (i.name == name && i.publisher == publisher)
                            {

                                var pv = Version.Parse(version);
                                var iv = Version.Parse(i.version);

                                if (iv < pv)
                                {
                                    toInstallFiles.Add(file);
                                }
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            toInstallFiles.Add(file);
                        }
                    }
                }
            }

            if (toInstallFiles.Count > 0)
            {
                doInstall.Checked = true;
            }
        }

        public override async Task<bool> DoInstall(ProgressBar progBar, Button displayButton, CancellationToken token)
        {
            if (doInstall.Checked)
            {
                displayButton.Text = "Installing VS Code Extensions";

                foreach (var file in toInstallFiles)
                {
                    var filePath = Path.Combine(resourceRoot, file);

                    var installResult = await TaskEx.Run(() =>
                    {
                        try
                        {
                            var startInfo = new ProcessStartInfo(Path.Combine(resourceRoot, "installcodeextension.bat"), filePath);
                            startInfo.UseShellExecute = false;
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            startInfo.CreateNoWindow = true;
                            startInfo.RedirectStandardOutput = true;
                            var proc = Process.Start(startInfo);
                            proc.WaitForExit();
                            return proc.StandardOutput.ReadToEnd();
                        }
                        catch (Exception e)
                        {
                            return null;
                        }
                    });
                    ;
                    MessageBox.Show(installResult);
                }

                displayButton.Text = "Finished Installing VS Code Extensions";
            }
            return true;
        }
    }
}
