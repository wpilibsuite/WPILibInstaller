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
        private ZipFile zipStore;
        private bool closeZip = false;
        private bool debugMode;

        public MainForm(ZipFile mainZipFile, bool debug)
        {
            zipStore = mainZipFile;
            debugMode = debug;
            InitializeComponent();
        }

        List<Checker> checkers = new List<Checker>();

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

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (closeZip)
            {
                zipStore.Close();
            }
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
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

            using (StreamReader reader = new StreamReader(zipStore.GetInputStream(upgradeEntry)))
            {
                upgradeConfigStr = await reader.ReadToEndAsync();
            }

            // Look for full config. Will not be there on upgrade
            var fullEntry = zipStore.FindEntry("installUtils/fullConfig.json", true);

            if (fullEntry == -1)
            {
                // Disable any full entry things
                gradleCheck.Enabled = false;
                javaCheck.Enabled = false;
                cppCheck.Enabled = false;
            }
            else
            {
                using (StreamReader reader = new StreamReader(zipStore.GetInputStream(fullEntry)))
                {
                    fullConfigStr = await reader.ReadToEndAsync();
                }
            }

            this.Enabled = true;
        }
    }
}
