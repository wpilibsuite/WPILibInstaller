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
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool debug = false;
#if DEBUG
            debug = true;
#endif

            foreach (var arg in args)
            {
                if (arg == "--debug")
                {
                    debug = true;
                    break;
                }
            }

            // Check to see if this executable is a zip
            var thisPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            using (FileStream fs = new FileStream(thisPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    using (ZipFile zfs = new ZipFile(fs))
                    {
                        zfs.IsStreamOwner = false;
                        Application.Run(new MainForm(zfs, debug));
                        return;
                    }
                }
                catch (ZipException zx)
                {
                    // Not a zip file. Let it close, and find our zip file
                }
            }

            Application.Run(new MainForm(null, debug));
        }
    }
}
