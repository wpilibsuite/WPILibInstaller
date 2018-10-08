using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
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


            // Check for Admin
            bool isAdmin = false;

            if (OSLoader.IsWindows())
            {

                using (WindowsIdentity identiy = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identiy);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }

                // If not admin, run admin check window
                if (!isAdmin)
                {
                    var adminForm = new AdminChecker();
                    Application.Run(adminForm);
                    // If set to admin, that means we need to restart. Just exit.
                    if (adminForm.Admin)
                    {
                        return;
                    }
                }
            }

            bool debug = args.Contains("--debug");
#if DEBUG
            debug = true;
#endif

            // Check to see if this executable is a zip
            var thisPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            using (FileStream fs = new FileStream(thisPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    using (ZipFile zfs = new ZipFile(fs))
                    {
                        zfs.IsStreamOwner = false;
                        ZipEntry filesEntry = zfs.GetEntry("files.zip");
                        var filesEntryStream = zfs.GetInputStream(filesEntry);
                        Application.Run(new MainForm(new ZipFile(filesEntryStream), debug, isAdmin));
                        return;
                    }
                }
                catch (ZipException)
                {
                    // Not a zip file. Let it close, and find our zip file
                }
            }

            Application.Run(new MainForm(null, debug, isAdmin));
        }
    }
}
