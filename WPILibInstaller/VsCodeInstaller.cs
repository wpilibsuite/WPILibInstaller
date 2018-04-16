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
    public class VsCodeInstaller : Checker
    {
        private CheckBox installCheckBox;
        private Button downloadVsCode;
        private Button mainInstallButton;
        private string resourcesFolder;
        private VsCodeConfig config;
        private string localPath = "";
        private ProgressBar progBar;
        private bool isDownloadingButton = false;
        private CancellationTokenSource source;

        public VsCodeInstaller(CheckBox installCheckBox, ProgressBar progBar, Button mainInstallButton, Button downloadVsCode, string resourcesFolder, VsCodeConfig config)
        {
            this.mainInstallButton = mainInstallButton;
            this.installCheckBox = installCheckBox;
            this.downloadVsCode = downloadVsCode;
            this.resourcesFolder = resourcesFolder;
            this.config = config;
            this.progBar = progBar;
            downloadVsCode.Click += OnDownloadButtonClick;
        }

        private async void OnDownloadButtonClick(object o, EventArgs e)
        {
            Button b = (Button)o;
            if (isDownloadingButton)
            {
                source?.Cancel();
                b.Enabled = false;
            }
            else
            {
                isDownloadingButton = true;
                b.Text = "Downloading, Click to Cancel";
                mainInstallButton.Enabled = false;
                source = new CancellationTokenSource();
                if (await DownloadVsCode(source.Token))
                {
                    b.Text = "Download successful";
                    b.Enabled = false;
                }
                else
                {
                    b.Text = "Download Failed";
                    b.Enabled = true;
                }
                isDownloadingButton = false;
                mainInstallButton.Enabled = false;
            }
        }

        private async Task<bool> DownloadVsCode(CancellationToken token)
        {
            using (var client = new HttpClientDownloadWithProgress(config.DownloadUrl, localPath))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => {
                    if (progressPercentage != null)
                    {
                        progBar.Value = (int)progressPercentage;
                    }
                };

                return await client.StartDownload(token);
            }
        }

        public override async Task CheckForInstall()
        {
            localPath = Path.Combine(resourcesFolder, config.Installer);
            bool doesDownloadExist = File.Exists(localPath);
            var codeAlreadyInstalled = await TaskEx.Run(() =>
            {
                try
                {
                    var codeStartInfo = new ProcessStartInfo("code", "-v");
                    codeStartInfo.CreateNoWindow = true;
                    codeStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    var codeProcess = Process.Start(codeStartInfo);
                    codeProcess.WaitForExit();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });

            installCheckBox.Checked = !codeAlreadyInstalled;
            downloadVsCode.Enabled = !doesDownloadExist;
            if (doesDownloadExist)
            {
                downloadVsCode.Text = "VS Download Found";
            }
            else
            {
                downloadVsCode.Text = "VS Download Not Found. Click to download";
            }
        }

        public override async Task<bool> DoInstall(ProgressBar progBar, Button displayButton, CancellationToken token)
        {
            if (!installCheckBox.Checked)
            {
                return false;
            }

            if (downloadVsCode.Enabled)
            {
                displayButton.Text = "Downloading VsCode";
                await DownloadVsCode(token);
            }

            displayButton.Text = "Installing VsCode";

            await TaskEx.Run(() =>
            {
                try
                {
                    var codeStartInfo = new ProcessStartInfo(localPath, config.InstallCommand);
                    codeStartInfo.CreateNoWindow = true;
                    codeStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    var codeProcess = Process.Start(codeStartInfo);
                    codeProcess.WaitForExit();
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed to install VsCode");
                }
            });

            return true;
        }
    }
}
