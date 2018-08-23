using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WPILibInstaller
{
    public class MavenConfig
    {
        public string Folder { get; set; }
    }

    public class UpgradeConfig
    {
        public string FrcYear { get; set; }
        public string ToolsFolder { get; set; }
        public MavenConfig Maven { get; set; }
    }
}
