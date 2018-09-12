using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using SharedCode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using File = System.IO.File;
using Newtonsoft.Json.Linq;

namespace WPILibInstaller
{
    public partial class MainForm : Form
    {
        private ZipFile zipStore;
        private bool closeZip = false;
        private bool debugMode;
        private bool adminMode;

        private UpgradeConfig upgradeConfig;
        private FullConfig fullConfig;
        private VsCodeConfig vsCodeConfig;

        private List<ExtractionIgnores> extractionControllers = new List<ExtractionIgnores>();

        private void SetVsCodeSettings(string frcHomePath)
        {
            //data\user-data\User
            var vsCodePath = Path.Combine(frcHomePath, "vscode");
            var settingsDir = Path.Combine(vsCodePath, "data", "user-data", "User");
            var settingsFile = Path.Combine(settingsDir, "settings.json");
            try
            {
                Directory.CreateDirectory(settingsDir);
            }
            catch (IOException)
            {

            }
            dynamic settingsJson = new JObject();
            if (File.Exists(settingsFile))
            {
                settingsJson = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(settingsFile));
            }

            if (!settingsJson.ContainsKey("java.home"))
            {
                settingsJson["java.home"] = Path.Combine(frcHomePath, "jdk");
            }

            if (!settingsJson.ContainsKey("terminal.integrated.env.windows"))
            {
                dynamic terminalProps = new JObject();

                terminalProps["JAVA_HOME"] = Path.Combine(frcHomePath, "jdk");
                terminalProps["PATH"] = Path.Combine(frcHomePath, "jdk", "bin") + ":${env:PATH}";

                settingsJson["terminal.integrated.env.windows"] = terminalProps;

            }
            else
            {
                dynamic terminalEnv = settingsJson["terminal.integrated.env.windows"];
                terminalEnv["JAVA_HOME"] = Path.Combine(frcHomePath, "jdk");
                string path = terminalEnv["PATH"];
                if (path == null)
                {
                    terminalEnv["PATH"] = Path.Combine(frcHomePath, "jdk", "bin") + ":${env:PATH}";
                }
                else
                {
                    var binPath = Path.Combine(frcHomePath, "jdk", "bin");
                    if (!path.Contains(binPath))
                    {
                        path = binPath + ";" + path;
                        terminalEnv["PATH"] = path;
                    }
                }
            }

            var serialized = JsonConvert.SerializeObject(settingsJson, Formatting.Indented);
            File.WriteAllText(settingsFile, serialized);
        }

        private void CreateCodeShortcuts(string frcHomePath)
        {
            {
                object shDesktop = "Desktop";
                WshShell shell = new WshShell();
                string shortcutAddress = shell.SpecialFolders.Item(ref shDesktop) + $"\\FRC VS Code {upgradeConfig.FrcYear}.lnk";
                IWshShortcut shortcut = shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "Shortcut for FRC VS Code";
                shortcut.TargetPath = Path.Combine(frcHomePath, "vscode", "Code.exe");
                shortcut.IconLocation = Path.Combine(frcHomePath, upgradeConfig.PathFolder, "wpilib-256.ico") + ",0";
                shortcut.Save();
            }
            {
                object shDesktop = "StartMenu";
                WshShell shell = new WshShell();
                string shortcutAddress = shell.SpecialFolders.Item(ref shDesktop) + $"\\FRC VS Code {upgradeConfig.FrcYear}.lnk";
                IWshShortcut shortcut = shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "Shortcut for FRC VS Code";
                shortcut.TargetPath = Path.Combine(frcHomePath, "vscode", "Code.exe");
                shortcut.IconLocation = Path.Combine(frcHomePath, upgradeConfig.PathFolder, "wpilib-256.ico") + ",0";
                shortcut.Save();
            }
        }

        private void CreateDevPromptShortcuts(string frcHomePath)
        {
            object shDesktop = "StartMenu";
            WshShell shell = new WshShell();
            string shortcutAddress = shell.SpecialFolders.Item(ref shDesktop) + $"\\FRC Developer Command Prompt {upgradeConfig.FrcYear}.lnk";
            IWshShortcut shortcut = shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Shortcut for FRC Development Command Prompt";
            shortcut.TargetPath = @"%comspec%";

            shortcut.Arguments = $"/k \"{Path.Combine(frcHomePath, "frccode", "frcvars.bat")}\"";
            object shDocuments = "MyDocuments";
            shortcut.WorkingDirectory = shell.SpecialFolders.Item(ref shDocuments);
            shortcut.IconLocation = Path.Combine(frcHomePath, upgradeConfig.PathFolder, "wpilib-256.ico") + ",0";
            shortcut.Save();
        }

        private void SetCppCompilerVariable(string frcHomePath, EnvironmentVariableTarget target)
        {
            var compilerPath = Path.Combine(frcHomePath, "gcc", "bin");

            var path = Environment.GetEnvironmentVariable("PATH", target);

            List<string> pathItems = path.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!pathItems.Contains(compilerPath))
            {
                pathItems.Add(compilerPath);
            }

            string newPath = string.Join(Path.PathSeparator.ToString(), pathItems);

            Environment.SetEnvironmentVariable("PATH", newPath, target);
        }

        private void SetVsCodeCmdVariables(string frcHomePath, EnvironmentVariableTarget target)
        {
            var codePath = Path.Combine(frcHomePath, upgradeConfig.PathFolder);

            var path = Environment.GetEnvironmentVariable("PATH", target);

            List<string> pathItems = path.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!pathItems.Contains(codePath))
            {
                pathItems.Add(codePath);
            }

            string newPath = string.Join(Path.PathSeparator.ToString(), pathItems);

            Environment.SetEnvironmentVariable("PATH", newPath, target);
        }

        private void SetJdkHome(string frcHomePath, EnvironmentVariableTarget target)
        {
            Environment.SetEnvironmentVariable("JDK_HOME", Path.Combine(frcHomePath, "jdk"), target);
        }

        private void SetFrcHome(string frcHomePath, string frcYear, EnvironmentVariableTarget target)
        {
            Environment.SetEnvironmentVariable($"FRC_{frcYear}_HOME", frcHomePath, target);
        }

        public MainForm(ZipFile mainZipFile, bool debug, bool isAdmin)
        {
            zipStore = mainZipFile;
            debugMode = debug;
            adminMode = isAdmin;
            InitializeComponent();
        }

        CancellationTokenSource source;
        bool isInstalling = false;

        private async void performInstallButton_Click(object sender, EventArgs e)
        {
            if (isInstalling)
            {
                source?.Cancel();
            }
            else
            {
                foreach (var c in Controls.OfType<CheckBox>())
                {
                    c.Enabled = false;
                }
                vscodeButton.Enabled = false;
                source = new CancellationTokenSource();
                performInstallButton.Enabled = false;
                isInstalling = true;

                List<string> ignoreDirs = new List<string>();

                foreach (var ignore in extractionControllers)
                {
                    ignore.AddToIgnoreList(ignoreDirs);
                }

                double totalCount = zipStore.Count;
                long currentCount = 0;

                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);

                var directory = new DirectoryInfo(documentsPath);

                // Now this should give you something like C:\Users\Public
                string commonPath = directory.Parent.FullName;

                string intoPath = Path.Combine(commonPath, $"frc{upgradeConfig.FrcYear}");

                // Extract zip
                foreach (ZipEntry entry in zipStore)
                {
                    double percentage = (currentCount / totalCount) * 100;
                    currentCount++;
                    progressBar1.Value = (int)percentage;

                    if (!entry.IsFile)
                    {
                        continue;
                    }

                    var entryName = entry.Name;
                    bool skip = false;
                    foreach (var ignore in ignoreDirs)
                    {
                        if (ignore.Contains(fullConfig.Gradle.ZipName))
                        {
                            ;
                        }
                        if (entryName.StartsWith(ignore))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                    {
                        continue;
                    }

                    Stream zipStream = zipStore.GetInputStream(entry);

                    string fullZipToPath = Path.Combine(intoPath, entryName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                    {
                        try
                        {
                            Directory.CreateDirectory(directoryName);
                        }
                        catch (IOException)
                        {

                        }
                    }

                    using (FileStream writer = File.Create(fullZipToPath))
                    {
                        await zipStream.CopyToAsync(writer);
                    }
                }

                if (gradleCheck.Checked)
                {
                    await GradleSetup.SetupGradle(fullConfig, intoPath);
                }

                if (toolsCheck.Checked)
                {
                    // Run tools fixer

                    ProcessStartInfo pstart = new ProcessStartInfo(Path.Combine(intoPath, upgradeConfig.Tools.Folder, upgradeConfig.Tools.UpdaterExe), "silent");
                    var p = Process.Start(pstart);
                    await TaskEx.Run(() =>
                    {
                        p.WaitForExit();
                    });
                }

                if (cppCheck.Checked)
                {
                    SetCppCompilerVariable(intoPath, EnvironmentVariableTarget.User);
                    if (adminMode)
                    {
                        SetCppCompilerVariable(intoPath, EnvironmentVariableTarget.Machine);
                    }
                }

                if (javaCheck.Checked)
                {
                    SetJdkHome(intoPath, EnvironmentVariableTarget.User);
                    if (adminMode)
                    {
                        SetJdkHome(intoPath, EnvironmentVariableTarget.Machine);
                    }
                }

                SetFrcHome(intoPath, upgradeConfig.FrcYear, EnvironmentVariableTarget.User);
                if (adminMode)
                {
                    SetFrcHome(intoPath, upgradeConfig.FrcYear, EnvironmentVariableTarget.Machine);
                }

                SetVsCodeCmdVariables(intoPath, EnvironmentVariableTarget.User);
                if (adminMode)
                {
                    SetVsCodeCmdVariables(intoPath, EnvironmentVariableTarget.Machine);
                }

                var vsToPath = Path.Combine(intoPath, "vscode");

                if (vscodeCheck.Checked)
                {
                    // Extract VS Code
                    using (FileStream fs = new FileStream(VsCodeZipFile, FileMode.Open, FileAccess.Read))
                    {
                        using (ZipFile zfs = new ZipFile(fs))
                        {
                            zfs.IsStreamOwner = false;
                            string vsName = Environment.Is64BitOperatingSystem ? vsCodeConfig.VsCode64Name : vsCodeConfig.VsCode32Name;
                            var entry = zfs.GetEntry($"download/{vsCodeConfig.VsCode64Name}");
                            var vsStream = zfs.GetInputStream(entry);
                            ZipFile zfsi = new ZipFile(vsStream);

                            foreach (ZipEntry vsEntry in zfsi)
                            {
                                if (!vsEntry.IsFile)
                                {
                                    continue;
                                }

                                Stream zipStream = zfsi.GetInputStream(vsEntry);
                                var entryName = vsEntry.Name;

                                string fullZipToPath = Path.Combine(vsToPath, entryName);
                                string directoryName = Path.GetDirectoryName(fullZipToPath);
                                if (directoryName.Length > 0)
                                {
                                    try
                                    {
                                        Directory.CreateDirectory(directoryName);
                                    }
                                    catch (IOException)
                                    {

                                    }
                                }

                                using (FileStream writer = File.Create(fullZipToPath))
                                {
                                    await zipStream.CopyToAsync(writer);
                                }
                            }
                        }
                    }
                    var dataFolder = Path.Combine(vsToPath, "data");
                    Directory.CreateDirectory(dataFolder);

                    var binFolder = Path.Combine(vsToPath, "bin");
                    var codeFolder = Path.Combine(intoPath, upgradeConfig.PathFolder);

                    File.Copy(Path.Combine(binFolder, "code"), Path.Combine(codeFolder, "frccode2019"), true);
                    File.Copy(Path.Combine(binFolder, "code.bat"), Path.Combine(codeFolder, "frccode2019.bat"), true);

                    CreateCodeShortcuts(intoPath);
                    SetVsCodeSettings(intoPath);

                }

                if (vscodeExtCheckBox.Checked)
                {
                    var tmpVsixDir = Path.Combine(intoPath, "tmp");
                    Directory.CreateDirectory(tmpVsixDir);



                    // Load extensions from VS Code

                    //Directory.Delete(tmpVsixDir, true);
                }


                if (wpilibCheck.Checked)
                {
                    // Run maven fixer

                    ProcessStartInfo pstart = new ProcessStartInfo(Path.Combine(intoPath, upgradeConfig.Maven.Folder, upgradeConfig.Maven.MetaDataFixerExe), "silent");
                    var p = Process.Start(pstart);
                    await TaskEx.Run(() =>
                    {
                        p.WaitForExit();
                    });
                }

                CreateDevPromptShortcuts(intoPath);

                isInstalling = false;
                performInstallButton.Enabled = false;
                performInstallButton.Text = "Finished Install";
                progressBar1.Value = 0;
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (closeZip)
            {
                zipStore.Close();
            }
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            vscodeButton.Enabled = false;
            vscodeCheck.Enabled = false;
            this.Enabled = false;
            if (zipStore == null)
            {
                if (debugMode)
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Title = "Select the installer zip";
                    var res = ofd.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read);
                        zipStore = new ZipFile(fs);
                        zipStore.IsStreamOwner = true;
                        closeZip = true;
                    }
                    else
                    {
                        MessageBox.Show("File Error. Please select a zip file.");
                        Application.Exit();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("File Error. Try redownloading the file, and if this error continues contact WPILib support.");
                    Application.Exit();
                    return;
                }
            }



            // Look for upgrade config. Should always be there.
            var upgradeEntry = zipStore.FindEntry("installUtils/upgradeConfig.json", true);

            if (upgradeEntry == -1)
            {
                // Error
                MessageBox.Show("File Error?");
                Application.Exit();
                return;
            }

            string upgradeConfigStr = "";
            string fullConfigStr = "";
            string vsConfigStr = "";

            using (StreamReader reader = new StreamReader(zipStore.GetInputStream(upgradeEntry)))
            {
                upgradeConfigStr = await reader.ReadToEndAsync();
                upgradeConfig = JsonConvert.DeserializeObject<UpgradeConfig>(upgradeConfigStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                });
                extractionControllers.Add(new ExtractionIgnores(toolsCheck, upgradeConfig.Tools.Folder, false));
                extractionControllers.Add(new ExtractionIgnores(wpilibCheck, upgradeConfig.Maven.Folder, false));
            }

            // Look for VS Code config. Should always be there.
            var vsCodeEntry = zipStore.FindEntry("installUtils/vscodeConfig.json", true);

            if (vsCodeEntry == -1)
            {
                // Error
                MessageBox.Show("File Error?");
                Application.Exit();
                return;
            }

            using (StreamReader reader = new StreamReader(zipStore.GetInputStream(vsCodeEntry)))
            {
                vsConfigStr = await reader.ReadToEndAsync();
                vsCodeConfig = JsonConvert.DeserializeObject<VsCodeConfig>(vsConfigStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                });
            }

            vscodeButton.Enabled = true;

            // Look for full config. Will not be there on upgrade
            var fullEntry = zipStore.FindEntry("installUtils/fullConfig.json", true);

            if (fullEntry == -1)
            {
                // Disable any full entry things
                javaCheck.Enabled = false;
                javaCheck.Checked = false;
                cppCheck.Enabled = false;
                cppCheck.Checked = false;
            }
            else
            {
                using (StreamReader reader = new StreamReader(zipStore.GetInputStream(fullEntry)))
                {
                    fullConfigStr = await reader.ReadToEndAsync();
                    fullConfig = JsonConvert.DeserializeObject<FullConfig>(fullConfigStr, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Error,
                    });

                    extractionControllers.Add(new ExtractionIgnores(cppCheck, fullConfig.CppToolchain.Directory, false));

                    extractionControllers.Add(new ExtractionIgnores(gradleCheck, "installUtils/" + fullConfig.Gradle.ZipName, true));
                }

                var jdkEntry = zipStore.FindEntry("installUtils/jdkConfig.json", true);
                if (jdkEntry == -1)
                {
                    // Error
                    MessageBox.Show("File Error?");
                    Application.Exit();
                    return;
                }
                else
                {
                    using (StreamReader reader = new StreamReader(zipStore.GetInputStream(jdkEntry)))
                    {
                        var jdkConfigStr = await reader.ReadToEndAsync();
                        var jdkConfig = JsonConvert.DeserializeObject<JdkConfig>(jdkConfigStr, new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Error,
                        });

                        var is32Bit = !Environment.Is64BitOperatingSystem;
                        if (!is32Bit && jdkConfig.Is32Bit)
                        {
                            // Error, need 64 bit
                            MessageBox.Show("You need the 64 bit full installer for this system");
                            Application.Exit();
                            return;
                        }
                        else if (is32Bit && !jdkConfig.Is32Bit)
                        {
                            // Error, need 32 bit
                            MessageBox.Show("You need the 32 bit full installer for this system");
                            Application.Exit();
                            return;
                        }
                        else
                        {
                            extractionControllers.Add(new ExtractionIgnores(javaCheck, jdkConfig.Folder, false));
                        }
                    }

                }
            }

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);

            var directory = new DirectoryInfo(documentsPath);

            // Now this should give you something like C:\Users\Public
            string commonPath = directory.Parent.FullName;

            VSCodeInstall vsi = new VSCodeInstall(Path.Combine(commonPath, $"frc{upgradeConfig.FrcYear}", "vscode"));

            if (vsi.IsInstalled())
            {
                var version = await vsi.GetVsCodeVersion();
                var extensions = await vsi.GetExtensions();

                // Get WPILib extension
            }



            this.performInstallButton.Enabled = true;
            this.performInstallButton.Visible = true;
            this.Enabled = true;
        }

        private string VsCodeZipFile;

        private void vscodeButton_Click(object sender, EventArgs e)
        {
            Selector selector = new Selector(vsCodeConfig);
            this.Enabled = false;
            selector.ShowDialog();
            this.Enabled = true;
            VsCodeZipFile = selector.ZipLocation;
            if (!string.IsNullOrWhiteSpace(VsCodeZipFile))
            {
                vscodeExtCheckBox.Enabled = true;
                vscodeExtCheckBox.Checked = true;
                vscodeCheck.Enabled = true;
                vscodeCheck.Checked = true;
            }
        }
    }
}
