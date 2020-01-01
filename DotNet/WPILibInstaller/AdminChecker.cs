using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WPILibInstaller
{
    public partial class AdminChecker : Form
    {
        [DllImport("user32")]
        private static extern uint SendMessage
    (IntPtr hWnd, uint msg, uint wParam, uint lParam);

        private const int BCM_FIRST = 0x1600; //Normal button
        private const int BCM_SETSHIELD = (BCM_FIRST + 0x000C); //Elevated button

        public bool Admin { get; set; }

        public AdminChecker()
        {
            InitializeComponent();
            allUsersButton.FlatStyle = FlatStyle.System;
            SendMessage(allUsersButton.Handle, BCM_SETSHIELD, 0, 0xFFFFFFFF);
        }

        private async Task<long> CopyBytesAsync(long bytesRequired, Stream inStream, Stream outStream)
        {
            long readSoFar = 0L;
            var buffer = new byte[64 * 1024];
            do
            {
                var toRead = Math.Min(bytesRequired - readSoFar, buffer.Length);
                var readNow = await inStream.ReadAsync(buffer, 0, (int)toRead);
                if (readNow == 0)
                    break; // End of stream
                await outStream.WriteAsync(buffer, 0, readNow);
                readSoFar += readNow;
            } while (readSoFar < bytesRequired);
            return readSoFar;
        }

        private async void allUsersButton_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            // Check to see if we are zip. If so we need to extract ourselves.
            var thisPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            using (FileStream fs = new FileStream(thisPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    long extractSize = 0;
                    using (ZipFile zfs = new ZipFile(fs))
                    {
                        zfs.IsStreamOwner = false;
                        ZipEntry filesEntry = zfs.GetEntry("WPILibSuite.exe");
                        // Extract just the size of the entry.
                        extractSize = filesEntry.Size;
                    }
                    fs.Seek(0, SeekOrigin.Begin);
                    var parent = Path.GetDirectoryName(thisPath);
                    var newFile = Path.Combine(parent, "WPILibInstallerAdminTemp.exe");
                    using (FileStream adminFile = new FileStream(newFile, FileMode.Create))
                    {
                        await CopyBytesAsync(extractSize, fs, adminFile);
                    }
                    StartSubProc(newFile, thisPath);
                    this.Enabled = true;
                    return;
                }
                catch (ZipException)
                {
                    // Not a zip file
                }
            }

            StartSubProc(Application.ExecutablePath, Application.ExecutablePath);
            this.Enabled = true;
        }

        private void StartSubProc(string fileToRun, string argument)
        {
            Admin = true;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = fileToRun;
            startInfo.Arguments = $"runFile:\"{argument}\"";
            startInfo.Verb = "runas";
            try
            {
                Process p = Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                return;
            }

            Application.Exit();
        }

        private void currentUserButton_Click(object sender, EventArgs e)
        {
            Admin = false;
            this.Close();
        }
    }
}
