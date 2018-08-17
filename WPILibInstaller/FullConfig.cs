using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WPILibInstaller
{
    public class GradleConfig
    {
        public string Hash { get; set; }
        public string ZipName { get; set; }
        public string ExtractLocation { get; set; }
    }

    public class CppToolchainConfig
    {
        public string Version { get; set; }
        public string Directory { get; set; }
    }

    public class JdksConfig
    {
        [JsonProperty("32BitFolder")]
        public string Folder32Bit { get; set; }
        [JsonProperty("32BitVersion")]
        public string Version32Bit { get; set; }
        [JsonProperty("64BitFolder")]
        public string Folder64Bit { get; set; }
        [JsonProperty("64BitVersion")]
        public string Version64Bit { get; set; }
    }

    public class FullConfig
    {
        public GradleConfig Gradle { get; set; }
        public CppToolchainConfig CppToolchain { get; set; }
        public JdksConfig Jdks { get; set; }
    }
}
