using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ToolsUpdater
{
    class Program
    {
        const string BatchFileBase =
@"@echo off
";
        static void Main(string[] args)
        {
            // Get location of EXE
            var exeFullPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            // Get directory EXE is in
            var exePath = Path.GetDirectoryName(exeFullPath);

            var jsonPath = Path.Combine(exePath, "tools.json");

            jsonPath = @"C:\Users\thadh\Documents\GitHub\thadhouse\WPILibInstaller\build\tools.json";

            var jsonContents = File.ReadAllText(jsonPath);

            var tools = JsonConvert.DeserializeObject<ToolsConfig[]>(jsonContents);

            var mavenFolder = Path.Combine(Path.GetDirectoryName(exePath), "maven");

            mavenFolder = @"C:\Users\thadh\Documents\GitHub\thadhouse\WPILibInstaller\offline-repository";

            Parallel.ForEach(tools, tool =>
            {
                var artifactFileName = $"{tool.Artifact.ArtifactId}-{tool.Artifact.Version}";
                if (!string.IsNullOrWhiteSpace(tool.Artifact.Classifier))
                {
                    artifactFileName += $"-{tool.Artifact.Classifier}";
                }
                artifactFileName += $".{tool.Artifact.Extension}";
                var artifactPath = Path.Combine(mavenFolder, tool.Artifact.GroupId.Replace('.', Path.DirectorySeparatorChar), tool.Artifact.ArtifactId, tool.Artifact.Version, artifactFileName);
                if (File.Exists(artifactPath))
                {
                    File.Copy(artifactPath, Path.Combine(exePath, tool.Name + ".jar"), true);
                    //File.WriteAllText(Path.Combine(exePath, tool.Name + ".bat"), "Hello World");
                }
            });

            // If silent, return early, otherwise show message
            if (args.Contains("silent"))
            {
                return;
            }
            MessageBox.Show("Tools Successfully Updated");
        }
    }
}
