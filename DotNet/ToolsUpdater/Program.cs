using Newtonsoft.Json;
using SharedCode;
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
        static void Main(string[] args)
        {
            // Get location of EXE
            var exeFullPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            // Get directory EXE is in
            var exePath = Path.GetDirectoryName(exeFullPath);

            var jsonPath = Path.Combine(exePath, "tools.json");

            var jsonContents = File.ReadAllText(jsonPath);

            var tools = JsonConvert.DeserializeObject<ToolConfig[]>(jsonContents);

            var mavenFolder = Path.Combine(Path.GetDirectoryName(exePath), "maven");

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
                    File.Copy(Path.Combine(exePath, "ScriptBase.vbs"), Path.Combine(exePath, tool.Name + ".vbs"));
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
