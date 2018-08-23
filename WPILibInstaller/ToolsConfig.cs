using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WPILibInstaller
{
    public class ArtifactConfig
    {
        [JsonProperty("classifier")]
        public string Classifier { get; set; }
        [JsonProperty("extension")]
        public string Cxtension { get; set; }
        [JsonProperty("groupId")]
        public string GroupId { get; set; }
        [JsonProperty("artifactId")]
        public string ArtifactId { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class ToolsConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("jar")]
        public string Jar { get; set; }
        [JsonProperty("artifact")]
        public ArtifactConfig Artifact { get; set; }
    }
}
