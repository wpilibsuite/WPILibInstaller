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
        private const string ResourcesFolder = "Resources";

        public MainForm()
        {
            InitializeComponent();
        }

        List<Checker> checkers = new List<Checker>();

        private async void Form1_Load(object sender, EventArgs e)
        {
            Path.Combine(ResourcesFolder, "config.json");
            string jsonContents = "";
            try
            {
                jsonContents = File.ReadAllText(Path.Combine(ResourcesFolder, "config.json"));
            }
            catch (IOException)
            {
                MessageBox.Show("Coult not find settings file. Contact WPILib");
                this.Close();
            }
            Config config = JsonConvert.DeserializeObject<Config>(jsonContents);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var result = await client.GetStringAsync("http://first.wpi.edu/FRC/roborio/wpilib.gpg.key");
                    dynamic resultParse = JsonConvert.DeserializeObject(result);
                    string newestVersion = resultParse.NewestVersion;
                    if (newestVersion != config.CurrentVersion)
                    {
                        MessageBox.Show("This is not the newest version");
                    }
                    ;
                }
                catch (Exception ex)
                {

                }
            }

            checkers.Add(new VsCodeInstaller(vscodeCheck, progressBar1, performInstallButton, vscodeButton, ResourcesFolder, config.VsCode));
            checkers.Add(new JavaInstaller(javaCheck, config.Java, ResourcesFolder, config.DefaultInstallLocation));
            checkers.Add(new GradleInstaller(gradleCheck, config.Gradle, ResourcesFolder, config.DefaultInstallLocation));
            checkers.Add(new CppInstaller(cppCheck, config.CppCompiler, ResourcesFolder, config.DefaultInstallLocation));
            checkers.Add(new EnvironmentSetters(allUsers, config.DefaultInstallLocation, ResourcesFolder, config.Year));

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
