using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedCode
{

    public class VsCodeConfig
    {
        public string VsCode32Url { get; set; }
        public string VsCode32Name { get; set; }
        public string VsCode64Url { get; set; }
        public string VsCode64Name { get; set; }
        public string cppUrl { get; set; }
        public string cppVsix { get; set; }
        public string javaDebugUrl { get; set; }
        public string javaDebugVsix { get; set; }
        public string javaLangUrl { get; set; }
        public string javaLangVsix { get; set; }
        public string[] extensions { get; set; }
    }
}
