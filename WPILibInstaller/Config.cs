using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WPILibInstaller
{
    public class Config
    {
        public string DefaultInstallLocation { get; set; }
        public string Year { get; set; }
        public string CurrentVersion { get; set; }
        public CppCompilerConfig CppCompiler { get; set; }
        public VsCodeConfig VsCode { get; set; }
        public JavaConfig Java { get; set; }
        public List<string> VsCodeExtensions { get; set; }
        public GradleConfig Gradle { get; set; }
    }

    public class CppCompilerConfig
    {
        public string Version { get; set; }
        public string Zip { get; set; }
    }

    public class VsCodeConfig
    {
        public string DownloadUrl { get; set; }
        public string Installer { get; set; }
        public string InstallCommand { get; set; }
    }

    public class JavaConfig
    {
        public string Version { get; set; }
        public string Zip { get; set; }
    }

    public class VsCodeExtensionsConfig
    {
        public string Version { get; set; }
        public string Zip { get; set; }
    }

    public class GradleConfig
    {
        public string FolderName { get; set; }
        public string Hash { get; set; }
        public string Zip { get; set; }
    }
}
