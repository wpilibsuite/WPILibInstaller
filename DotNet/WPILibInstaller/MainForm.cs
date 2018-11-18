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
#if !MAC && !LINUX
using IWshRuntimeLibrary;
#endif
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
        private string frcHome;

        private UpgradeConfig upgradeConfig;
        private FullConfig fullConfig;
        private VsCodeConfig vsCodeConfig;

        private List<ExtractionIgnores> extractionControllers = new List<ExtractionIgnores>();

        private async Task HandleVsCodeExtensions(string frcHomePath)
        {
            if (!vsCodeWpiExtCheck.Checked)
            {
                return;
            }

            var codeBatFile = Path.Combine(frcHomePath, "vscode", "bin", "code.cmd");

            // Load existing extensions
            var versions = await Task.Run(() =>
             {
                 ProcessStartInfo startInfo = new ProcessStartInfo(codeBatFile, "--list-extensions --show-versions");
                 startInfo.UseShellExecute = false;
                 startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                 startInfo.CreateNoWindow = true;
                 startInfo.RedirectStandardOutput = true;
                 var proc = Process.Start(startInfo);
                 proc.WaitForExit();
                 List<(string name, string version)> lines = new List<(string name, string version)>();
                 while (true)
                 {
                     string line = proc.StandardOutput.ReadLine();
                     if (line == null)
                     {
                         return lines;
                     }

                     if (line.Contains("@"))
                     {
                         var split = line.Split('@');
                         lines.Add((split[0], split[1]));
                     }
                 }
             });

            List<(Extension extension, int sortOrder)> availableToInstall = new List<(Extension extension, int sortOrder)>();

            availableToInstall.Add((vsCodeConfig.WPILibExtension, int.MaxValue));
            for (int i = 0; i < vsCodeConfig.ThirdPartyExtensions.Length; i++)
            {
                availableToInstall.Add((vsCodeConfig.ThirdPartyExtensions[i], i));
            }

            var maybeUpdates = availableToInstall.Where(x => versions.Select(y => y.name).Contains((x.extension.Name))).ToList();
            var newInstall = availableToInstall.Except(maybeUpdates).ToList();

            var definitelyUpdate = maybeUpdates.Join(versions, x => x.extension.Name, y => y.name, (newVersion, existing) => (newVersion, existing))
                                          .Where(x => x.newVersion.extension.Version.CompareTo(x.existing.version) > 0).Select(x => x.newVersion);

            var installs = definitelyUpdate.Concat(newInstall).OrderBy(x => x.sortOrder).Select(x => x.extension).ToArray();

            performInstallButton.Text = "Installing Extensions";

            await Task.Run(() =>
            {
                int i = 0;
                double end = installs.Length;
                progressBar1.Value = 0;
                foreach (var item in installs)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(codeBatFile, "--install-extension " + Path.Combine(frcHomePath, "vsCodeExtensions", item.Vsix));
                    startInfo.UseShellExecute = false;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                    startInfo.RedirectStandardOutput = true;
                    var proc = Process.Start(startInfo);
                    proc.WaitForExit();
                    var data = proc.StandardOutput.ReadToEnd();
                    i++;
                    double percentage = (i / end) * 100;
                    if (percentage > 100) percentage = 100;
                    if (percentage < 0) percentage = 0;
                    progressBar1.Value = (int)percentage;
                    ;
                }
            });
        }

        private bool CheckForVsCode(string frcHomePath)
        {
            var codeBatFile = Path.Combine(frcHomePath, "vscode", "bin", "code.cmd");

            return File.Exists(codeBatFile);
        }

        private void setIfNotSet<T>(string key, T value, dynamic settingsJson)
        {
            if (!settingsJson.ContainsKey(key))
            {

                settingsJson[key] = value;
            }
        }

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

            setIfNotSet("java.home", Path.Combine(frcHomePath, "jdk"), settingsJson);
            setIfNotSet("extensions.autoUpdate", false, settingsJson);
            setIfNotSet("extensions.autoCheckUpdates", false, settingsJson);
            setIfNotSet("extensions.ignoreRecommendations", true, settingsJson);
            setIfNotSet("extensions.showRecommendationsOnlyOnDemand", false, settingsJson);
            setIfNotSet("update.channel", "none", settingsJson);
            setIfNotSet("update.showReleaseNotes", false, settingsJson);

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
                    terminalEnv["PATH"] = Path.Combine(frcHomePath, "jdk", "bin") + ";${env:PATH}";
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

        private async Task RunDotNetExecutable(string exe, params string[] args)
        {
            var assembly = Assembly.LoadFile(exe);
            var entryMethod = assembly.EntryPoint;
            await Task.Run(() =>
            {
                entryMethod.Invoke(null, new object[] { args });
            });
        }

        private async Task RunScriptExecutable(string script, params string[] args) {
            ProcessStartInfo pstart = new ProcessStartInfo(script, string.Join(" ", args));
            var p = Process.Start(pstart);
            await Task.Run(() =>
            {
                p.WaitForExit();
            });
        }

        private void CreateCodeShortcuts(string frcHomePath)
        {
#if !MAC && !LINUX
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
#endif
        }

        private void CreateDevPromptShortcuts(string frcHomePath)
        {
#if !MAC && !LINUX
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
#endif
        }

        private void SetCppCompilerVariable(string frcHomePath, EnvironmentVariableTarget target)
        {
            var compilerPath = Path.Combine(frcHomePath, fullConfig.CppToolchain.Directory, "bin");

            var path = Environment.GetEnvironmentVariable("PATH", target);

            if (path == null) {
                path = "";
            }

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

            if (path == null) {
                path = "";
            }

            List<string> pathItems = path.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!pathItems.Contains(codePath))
            {
                pathItems.Add(codePath);
            }

            string newPath = string.Join(Path.PathSeparator.ToString(), pathItems);

            Environment.SetEnvironmentVariable("PATH", newPath, target);
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
        bool isWindows = false;

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

                string intoPath = frcHome;

                // Extract zip
                foreach (ZipEntry entry in zipStore)
                {
                    double percentage = (currentCount / totalCount) * 100;
                    currentCount++;
                    if (percentage > 100) percentage = 100;
                    if (percentage < 0) percentage = 0;
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
                    await RunScriptExecutable(Path.Combine(intoPath, upgradeConfig.Tools.Folder, upgradeConfig.Tools.UpdaterExe), "silent");
                }

                if (cppCheck.Checked)
                {
                    SetCppCompilerVariable(intoPath, EnvironmentVariableTarget.User);
                    if (adminMode)
                    {
                        SetCppCompilerVariable(intoPath, EnvironmentVariableTarget.Machine);
                    }
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
                            var entry = zfs.GetEntry($"downloadvscodetmp/{vsName}");
                            if (entry == null) {
                                StringBuilder builder = new StringBuilder();
                                builder.AppendLine($"Expected to find {vsName} in zip, however did not.");
                                builder.AppendLine("Aborting. Contact WPILib for assistance.");
                                MessageBox.Show(builder.ToString());
                                Application.Exit();
                            }
                            var vsStream = zfs.GetInputStream(entry);
                            ZipFile zfsi = new ZipFile(vsStream);

                            performInstallButton.Text = "Installing VS Code";

                            totalCount = zfsi.Count;
                            currentCount = 0;

                            foreach (ZipEntry vsEntry in zfsi)
                            {
                                double percentage = (currentCount / totalCount) * 100;
                                currentCount++;
                                if (percentage > 100) percentage = 100;
                                if (percentage < 0) percentage = 0;
                                progressBar1.Value = (int)percentage;
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
                    try
                    {
                        Directory.CreateDirectory(dataFolder);
                    }
                    catch (IOException)
                    {

                    }

                    var binFolder = Path.Combine(vsToPath, "bin");
                    var codeFolder = Path.Combine(intoPath, upgradeConfig.PathFolder);

                    try
                    {
                        Directory.CreateDirectory(codeFolder);
                    }
                    catch (IOException)
                    {

                    }

                    CreateCodeShortcuts(intoPath);
                    SetVsCodeSettings(intoPath);

                }

                if (vsCodeWpiExtCheck.Checked)
                {
                    await HandleVsCodeExtensions(intoPath);
                }

                if (wpilibCheck.Checked)
                {
                    // Run maven fixer
                    await RunScriptExecutable(Path.Combine(intoPath, upgradeConfig.Maven.Folder, upgradeConfig.Maven.MetaDataFixerExe), "silent");
                }

                CreateDevPromptShortcuts(intoPath);

                isInstalling = false;
                performInstallButton.Enabled = false;
                performInstallButton.Text = "Finished Install";
                progressBar1.Value = 0;

                MessageBox.Show("Finished! Use Desktop Icon to Open VS Code");
                this.Close();
            }
        }

        //private async Task InstallVsRedistributable(string intoPath)
        //{
        //    string arg = "/install /passive /norestart";

        //    ProcessStartInfo startInfo = new ProcessStartInfo(Path.Combine(intoPath, "installUtils", fullConfig.Redist.File32), arg);
        //    Process proc = Process.Start(startInfo);
        //    proc.Start();
        //    await proc.WaitForExitAsync();

        //    if (upgradeConfig.InstallerType == UpgradeConfig.Windows64InstallerType)
        //    {
        //        startInfo = new ProcessStartInfo(Path.Combine(intoPath, "installUtils", fullConfig.Redist.File64), arg);
        //        proc = Process.Start(startInfo);
        //        proc.Start();
        //        await proc.WaitForExitAsync();
        //    }
        //}

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
            vscodeText.Visible = false;
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

                var osType = OSLoader.GetOsType();
                switch (upgradeConfig.InstallerType)
                {
                    case UpgradeConfig.LinuxInstallerType:
                        if (osType != OsType.Linux64)
                        {
                            MessageBox.Show("You need the Linux installer for this system");
                            Application.Exit();
                            return;
                        }
                        isWindows = false;
                        break;
                    case UpgradeConfig.MacInstallerType:
                        if (osType != OsType.MacOs64)
                        {
                            MessageBox.Show("You need the Mac installer for this system");
                            Application.Exit();
                            return;
                        }
                        isWindows = false;
                        break;
                    case UpgradeConfig.Windows32InstallerType:
                        if (osType != OsType.Windows32)
                        {
                            MessageBox.Show("You need the Windows32 installer for this system");
                            Application.Exit();
                            return;
                        }
                        isWindows = true;
                        break;
                    case UpgradeConfig.Windows64InstallerType:
                        if (osType != OsType.Windows64)
                        {
                            MessageBox.Show("You need the Windows64 installer for this system");
                            Application.Exit();
                            return;
                        }
                        isWindows = true;
                        break;
                    default:
                        MessageBox.Show("Unknown installer type?");
                        Application.Exit();
                        return;
                }
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


                        extractionControllers.Add(new ExtractionIgnores(javaCheck, jdkConfig.Folder, false));
                    }

                }
            }

            if (isWindows)
            {
                var publicFolder = Environment.GetEnvironmentVariable("PUBLIC");
                frcHome = Path.Combine(publicFolder, $"frc{upgradeConfig.FrcYear}");
            }
            else
            {
                var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                frcHome = Path.Combine(userFolder, $"frc{upgradeConfig.FrcYear}");
            }

            VSCodeInstall vsi = new VSCodeInstall(Path.Combine(frcHome, "vscode"));

            if (!vsi.IsInstalled())
            {
                vsCodeWpiExtCheck.Checked = false;
                vsCodeWpiExtCheck.Enabled = false;
            } else
            {
                vsCodeWpiExtCheck.Checked = true;
                vsCodeWpiExtCheck.Enabled = true;
            }



            this.performInstallButton.Enabled = true;
            this.performInstallButton.Visible = true;
            this.Enabled = true;
        }

        private string VsCodeZipFile;
        private bool vsCodeSelected = false;

        private void vscodeButton_Click(object sender, EventArgs e)
        {
            if (vsCodeSelected)
            {
                Process.Start("explorer.exe", "/select \"" + VsCodeZipFile + "\"");
                return;
            }
            Selector selector = new Selector(vsCodeConfig);
            this.Enabled = false;
            selector.ShowDialog();
            this.Enabled = true;
            VsCodeZipFile = selector.ZipLocation;
            if (!string.IsNullOrWhiteSpace(VsCodeZipFile))
            {
                vscodeCheck.Enabled = true;
                vscodeCheck.Checked = true;
                vscodeText.Visible = true;
                vsCodeSelected = true;
                vscodeButton.Text = "Open Downloaded\nFile.";
            }
        }

        private bool hasBeenCheckedOnce = false;

        private void vscodeCheck_CheckedChanged(object sender, EventArgs e)
        {
            vsCodeWpiExtCheck.Enabled = vscodeCheck.Checked;
            if (!hasBeenCheckedOnce && vscodeCheck.Checked)
            {
                hasBeenCheckedOnce = true;
                vsCodeWpiExtCheck.Checked = true;
            }
        }
    }
}
