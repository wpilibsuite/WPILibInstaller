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
    public class EnvironmentSetters : Checker
    {
        private string installPath;
        private string exeFullPath;
        private CheckBox allUsers;
        private string year;

        private const string exeName = "ElevatedPermissionSetter.exe";

        public EnvironmentSetters(CheckBox allUsers, string installPath, string resourceRoot, string year)
        {
            this.installPath = installPath;
            exeFullPath = Path.Combine(resourceRoot, exeName);
            this.allUsers = allUsers;
            this.year = year;
        }

        public override async Task CheckForInstall()
        {
            
        }

        public override async Task<bool> DoInstall(ProgressBar progBar, Button displayButton, CancellationToken token)
        {
            displayButton.Text = "Setting Environmental Variables";

            ProcessStartInfo startInfo = new ProcessStartInfo(exeFullPath);

            StringBuilder builder = new StringBuilder();
            if (allUsers.Checked)
            {
                builder.Append("ADMIN");
            }
            else
            {
                builder.Append("USER");
            }

            builder.Append($" {Path.PathSeparator}{installPath}");

            builder.Append(" ");

            builder.Append($"FRC_{year}_HOME:{installPath}");

            builder.Append(" ");

            builder.Append($"FRC_HOME:{installPath}");

            builder.Append(" ");

            startInfo.Arguments = builder.ToString();

            if (allUsers.Checked)
            {
                startInfo.Verb = "runas";
            }

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;

            await TaskEx.Run(() =>
            {
                var process = Process.Start(startInfo);
                process.WaitForExit();
            });

            displayButton.Text = "Set Environmental Variables";

            return true;
        }
    }
}
