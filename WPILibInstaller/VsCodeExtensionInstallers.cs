using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WPILibInstaller
{
    public class VsCodeExtensionInstallers : Checker
    {
        public override async Task CheckForInstall()
        {
            
        }

        public override async Task<bool> DoInstall(ProgressBar progBar, Button displayButton, CancellationToken token)
        {
            return true;
        }
    }
}
