using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedCode
{
    public class MavenConfig
    {
        public string Folder { get; set; }
        public string MetaDataFixerExe { get; set; }
    }

    public class ToolsConfig
    {
        public string Folder { get; set; }
        public string UpdaterExe { get; set; }
    }

    public class UpgradeConfig
    {
        public string FrcYear { get; set; }
        public MavenConfig Maven { get; set; }
        public ToolsConfig Tools { get; set; }
        public string PathFolder { get; set; }
    }
}
