﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedCode
{
    public class JdkConfig
    {
        [JsonProperty("tarFile")]
        public string TarFile { get; set; }
        [JsonProperty("folder")]
        public string Folder { get; set; }
    }
}
