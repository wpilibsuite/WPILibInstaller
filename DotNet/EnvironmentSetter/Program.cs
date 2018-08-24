using Newtonsoft.Json;
using SharedCode;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace EnvironmentSetter
{
    class Program
    {
        static void SetVariables(EnvironmentVariableTarget target, string year, string frcHomePath)
        {
            Environment.SetEnvironmentVariable("JAVA_HOME", Path.Combine(frcHomePath, "jdk"), target);
            Environment.SetEnvironmentVariable($"FRC_{year}_HOME", frcHomePath, target);
            var path = Environment.GetEnvironmentVariable("PATH", target);

            List<string> pathItems = path.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();


            // Add Compiler and VS Code items
            var compilerPath = Path.Combine(frcHomePath, "gcc", "bin");
            var codePath = Path.Combine(frcHomePath, "frccode");

            if (!pathItems.Contains(compilerPath))
            {
                pathItems.Add(compilerPath);
            }

            if (!pathItems.Contains(codePath))
            {
                pathItems.Add(codePath);
            }

            string newPath = string.Join(Path.PathSeparator.ToString(), pathItems);

            Environment.SetEnvironmentVariable("PATH", newPath, target);
        }

        static void Main(string[] args)
        {
            // Get location of EXE
            var exeFullPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            // Get directory EXE is in
            var exePath = Path.GetDirectoryName(exeFullPath);

            var frcHomePath = Path.GetDirectoryName(exePath);

            bool isAdmin = false;

            using (WindowsIdentity identiy = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identiy);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            var jsonText = File.ReadAllText(Path.Combine(exePath, "upgradeConfig.json"));
            var upgradeConfig = JsonConvert.DeserializeObject<UpgradeConfig>(jsonText);
            string year = upgradeConfig.FrcYear;

            if (isAdmin)
            {
                SetVariables(EnvironmentVariableTarget.Machine, year, frcHomePath);
                SetVariables(EnvironmentVariableTarget.User, year, frcHomePath);
            }
            else
            {
                string text = "Would you like to setup for All Users? Yes for All Users, No for just Current User.";
                string caption = "Environment Settings";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                var result = MessageBox.Show(text, caption, buttons);

                if (result == DialogResult.Yes)
                {
                    // Admin, fire up as admin and return
                    ProcessStartInfo startInfo = new ProcessStartInfo(exeFullPath, "silent")
                    {
                        Verb = "runas"
                    };

                    var proc = Process.Start(startInfo);

                    proc.WaitForExit();
                }
                else
                {
                    // User only
                    SetVariables(EnvironmentVariableTarget.User, year, frcHomePath);
                }
            }

            // If silent, return early, otherwise show message
            if (args.Contains("silent"))
            {
                return;
            }
            MessageBox.Show("Environment Successfully Updated");
        }
    }
}
