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
        private static readonly string BaseFileName = "OfflineVsCodeFiles";

        public static string GetFileName(string version)
        {
            return $"{BaseFileName}-{version}.zip";
        }

        private VsCodeConfig config;
        private string downloadDir = "downloadvscodetmp";
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


        public async Task<string> DownloadAndZipFiles(ProgressBar vs32Prog, ProgressBar vs64Prog, string version, CancellationToken token)
        {

            Directory.CreateDirectory(downloadDir);

            (bool success, string output)[] results = await Task.WhenAll(
                GetVsCode32Zip(vs32Prog, token, downloadDir),
                GetVsCode64Zip(vs64Prog, token, downloadDir));

            if (!results.All(x => x.success == true))
            {
                // Has failure.
                MessageBox.Show("Download Failure");
                return null;
            }

            string zipLoc = GetFileName(version);

            using (ZipFile newFile = ZipFile.Create(zipLoc))
            {
                newFile.BeginUpdate();
                foreach(var (success, output) in results)
                {
                    newFile.Add(output, CompressionMethod.Stored);
                }
                newFile.CommitUpdate();
                newFile.Close();
            }
            foreach (var (success, output) in results)
            {
                try
                {
                    File.Delete(output);
                }
                catch
                {

                }
            }
            try
            {
                Directory.Delete(downloadDir, true);
            }
            catch
            {

            }
            return zipLoc;
        }
    }
}
