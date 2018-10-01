using ICSharpCode.SharpZipLib.Zip;
using SharedCode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WPILibInstaller
{
    public class VsCodeFiles
    {
        private VsCodeConfig config;
        private string downloadDir = "download";
        public VsCodeFiles(VsCodeConfig config)
        {
            this.config = config;
        }



        private async Task<(bool, string)> GetVsCode32Zip(ProgressBar progBar, CancellationToken token, string tmpDir)
        {
            var output = Path.Combine(tmpDir, config.VsCode32Name);
            using (var client = new HttpClientDownloadWithProgress(config.VsCode32Url, output))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => {
                    if (progressPercentage != null)
                    {
                        progBar.Value = (int)progressPercentage;
                    }
                };

                return (await client.StartDownload(token), output);
            }
        }

        private async Task<(bool, string)> GetVsCode64Zip(ProgressBar progBar, CancellationToken token, string tmpDir)
        {
            var output = Path.Combine(tmpDir, config.VsCode64Name);
            using (var client = new HttpClientDownloadWithProgress(config.VsCode64Url, output))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => {
                    if (progressPercentage != null)
                    {
                        progBar.Value = (int)progressPercentage;
                    }
                };

                return (await client.StartDownload(token), output);
            }
        }


        public async Task<string> DownloadAndZipFiles(ProgressBar vs32Prog, ProgressBar vs64Prog, CancellationToken token)
        {

            Directory.CreateDirectory(downloadDir);

            (bool success, string output)[] results = await TaskEx.WhenAll(
                GetVsCode32Zip(vs32Prog, token, downloadDir),
                GetVsCode64Zip(vs64Prog, token, downloadDir));

            if (!results.All(x => x.success == true))
            {
                // Has failure.
                MessageBox.Show("Download Failure");
                return null;
            }

            string zipLoc = "OfflineVsCodeFiles.zip";

            using (ZipFile newFile = ZipFile.Create("OfflineVsCodeFiles.zip"))
            {
                newFile.BeginUpdate();
                foreach(var (success, output) in results)
                {
                    newFile.Add(output, CompressionMethod.Stored);
                }
                newFile.CommitUpdate();
                newFile.Close();
            }
            return zipLoc;
        }
    }
}
