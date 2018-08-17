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
        private readonly ZipFile zipStore;

        public MainForm(ZipFile mainZipFile)
        {
            zipStore = mainZipFile;
            InitializeComponent();
        }

        List<Checker> checkers = new List<Checker>();

        private async void Form1_Load(object sender, EventArgs e)
        {
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

            using (StreamReader reader = new StreamReader(zipStore.GetInputStream(upgradeEntry)))
            {
                upgradeConfigStr = await reader.ReadToEndAsync();
            }

            // Look for full config. Will not be there on upgrade
            var fullEntry = zipStore.FindEntry("installUtils/fullConfig.json", true);

            if (fullEntry == -1)
            {
                // Disable any full entry things
            }
            else
            {
                using (StreamReader reader = new StreamReader(zipStore.GetInputStream(fullEntry)))
                {
                    fullConfigStr = await reader.ReadToEndAsync();
                }
            }

            ;



            //Path.Combine(ResourcesFolder, "config.json");
            //string jsonContents = "";
            //try
            //{
            //    jsonContents = File.ReadAllText(Path.Combine(ResourcesFolder, "config.json"));
            //}
            //catch (IOException)
            //{
            //    MessageBox.Show("Coult not find settings file. Contact WPILib");
            //    this.Close();
            //}
            //Config config = JsonConvert.DeserializeObject<Config>(jsonContents);

            //using (HttpClient client = new HttpClient())
            //{
            //    try
            //    {
            //        var result = await client.GetStringAsync("http://first.wpi.edu/FRC/roborio/wpilib.gpg.key");
            //        dynamic resultParse = JsonConvert.DeserializeObject(result);
            //        string newestVersion = resultParse.NewestVersion;
            //        if (newestVersion != config.CurrentVersion)
            //        {
            //            MessageBox.Show("This is not the newest version");
            //        }
            //        ;
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //}

            //checkers.Add(new VsCodeInstaller(vscodeCheck, progressBar1, performInstallButton, vscodeButton, ResourcesFolder, config.VsCode));
            //checkers.Add(new JavaInstaller(javaCheck, config.Java, ResourcesFolder, config.DefaultInstallLocation));
            //checkers.Add(new GradleInstaller(gradleCheck, config.Gradle, ResourcesFolder, config.DefaultInstallLocation));
            //checkers.Add(new CppInstaller(cppCheck, config.CppCompiler, ResourcesFolder, config.DefaultInstallLocation));
            //checkers.Add(new EnvironmentSetters(allUsers, config.DefaultInstallLocation, ResourcesFolder, config.Year));
            //checkers.Add(new VsCodeExtensionInstallers(vscodeExtCheckBox, config.VsCodeExtensions, ResourcesFolder));

            await TaskEx.WhenAll(checkers.Select(x => x.CheckForInstall()));

            performInstallButton.Visible = true;
        }

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



                foreach (var checker in checkers)
                {
                    await checker.DoInstall(progressBar1, performInstallButton, source.Token);
                }
                isInstalling = false;
                performInstallButton.Enabled = false;
                performInstallButton.Text = "Finished Install";
                progressBar1.Value = 0;
            }
        }
    }
}
