using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPILibInstaller
{
    public static class Extensions
    {
        public static async Task WaitForExitAsync(this Process process)
        {
            await TaskEx.Run(() =>
            {
                process.WaitForExit();
            });
        }
    }
}
