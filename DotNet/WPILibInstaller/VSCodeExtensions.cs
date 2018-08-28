using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPILibInstaller
{
    public class VSCodeInstall
    {
        private string codePath;
        private string vsCodeVersion;
        private List<(string name, string version)> extensionVersions;

        public VSCodeInstall(string codePath)
        {
            this.codePath = codePath;
        }

        public bool IsInstalled()
        {
            return File.Exists(Path.Combine(codePath, "bin", "code.cmd"));
        }

        public async Task<string> GetVsCodeVersion()
        {
            if (vsCodeVersion != null)
            {
                return vsCodeVersion;
            }
            var startInfo = new ProcessStartInfo(Path.Combine(codePath, "bin", "code.cmd"), "--version");
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            var proc = Process.Start(startInfo);
            await proc.WaitForExitAsync();
            vsCodeVersion = await proc.StandardOutput.ReadLineAsync();
            return vsCodeVersion;
        }

        public async Task<List<(string name, string version)>> GetExtensions()
        {
            if (extensionVersions != null)
            {
                return extensionVersions;
            }
            var startInfo = new ProcessStartInfo(Path.Combine(codePath, "bin", "code.cmd"), "--list-extensions --show-versions");
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            var proc = Process.Start(startInfo);
            await proc.WaitForExitAsync();
            string itemsStr = await proc.StandardOutput.ReadToEndAsync();
            extensionVersions = itemsStr.Replace("\r\n", "\n").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                           .Select(x => x.Split('@')).Select(x => (x[0], x[1])).ToList();
            return extensionVersions;
        }
    }
}
