using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedCode
{
    public class JdkConfig
    {
        [JsonProperty("is32")]
        public bool Is32Bit { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("folder")]
        public string Folder { get; set; }
    }
}
