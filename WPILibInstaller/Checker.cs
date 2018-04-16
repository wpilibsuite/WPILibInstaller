using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WPILibInstaller
{
    public abstract class Checker
    {
        public abstract Task CheckForInstall();

        public abstract Task<bool> DoInstall(ProgressBar progBar, Button displayButton, CancellationToken token);
    }
}
