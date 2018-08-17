using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WPILibInstaller
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check to see if this executable is a zip
            var thisPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            using (FileStream fs = new FileStream(thisPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    using (ZipFile zfs = new ZipFile(fs))
                    {
                        zfs.IsStreamOwner = false;
                        Application.Run(new MainForm(zfs));
                        return;
                    }
                }
                catch (ZipException zx)
                {
                    // Not a zip file. Let it close, and find our zip file
                }
            }


#if DEBUG
            // If here, and in debug, we need to select our zip
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select the installer zip";
            var res = ofd.ShowDialog();
            if (res == DialogResult.OK)
            {
                using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                {
                    using (ZipFile zfs = new ZipFile(fs))
                    {
                        zfs.IsStreamOwner = false;
                        Application.Run(new MainForm(zfs));
                        return;
                    }
                }
            }

#endif
            // If error, give support message
            MessageBox.Show("File Error. Try redownloading the file, and if this error continues contact WPILib support.");
        }
    }
}
