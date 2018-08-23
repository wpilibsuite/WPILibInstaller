using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
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

namespace WPILibInstaller
{
    public partial class MainForm : Form
    {
        private ZipFile zipStore;
        private bool closeZip = false;
        private bool debugMode;

        private UpgradeConfig upgradeConfig;
        private FullConfig fullConfig;
        private VsCodeConfig vsCodeConfig;

        private List<ExtractionIgnores> extractionControllers = new List<ExtractionIgnores>();

        public MainForm(ZipFile mainZipFile, bool debug)
        {
            zipStore = mainZipFile;
            debugMode = debug;
            InitializeComponent();
        }

        //List<Checker> checkers = new List<Checker>();

        private void javaButton_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com");
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
            vscodeExtCheckBox.Enabled = false;
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
                extractionControllers.Add(new ExtractionIgnores(toolsCheck, upgradeConfig.ToolsFolder, false));
                extractionControllers.Add(new ExtractionIgnores(wpilibCheck, "maven", false)); // TODO make this a real folder
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
                cppCheck.Enabled = false;
            }
            else
            {
                using (StreamReader reader = new StreamReader(zipStore.GetInputStream(fullEntry)))
                {
                    fullConfigStr = await reader.ReadToEndAsync();
                    fullConfig = JsonConvert.DeserializeObject<FullConfig>(fullConfigStr, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Error
                    });
                    if (Environment.Is64BitOperatingSystem)
                    {
                        extractionControllers.Add(new ExtractionIgnores(fullConfig.Jdks.Folder32Bit, false));
                        extractionControllers.Add(new ExtractionIgnores(javaCheck, fullConfig.Jdks.Folder64Bit, false));
                    }
                    else
                    {
                        extractionControllers.Add(new ExtractionIgnores(fullConfig.Jdks.Folder64Bit, false));
                        extractionControllers.Add(new ExtractionIgnores(javaCheck, fullConfig.Jdks.Folder32Bit, false));
                    }

                    extractionControllers.Add(new ExtractionIgnores(cppCheck, fullConfig.CppToolchain.Directory, false));

                    extractionControllers.Add(new ExtractionIgnores(gradleCheck, "installUtils/" + fullConfig.Gradle.ZipName, true));
                }
            }

            this.performInstallButton.Enabled = true;
            this.performInstallButton.Visible = true;
            this.Enabled = true;
        }

        private async void vscodeButton_Click(object sender, EventArgs e)
        {
            VsCodeFiles vsf = new VsCodeFiles(vsCodeConfig);

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            await vsf.DownloadAndZipFiles(progressBar2, progressBar3, progressBar4, progressBar5, progressBar6, CancellationToken.None);

            MessageBox.Show("Done!");
        }
    }
}
