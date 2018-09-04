using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        private void allUsersButton_Click(object sender, EventArgs e)
        {
            Admin = true;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
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
