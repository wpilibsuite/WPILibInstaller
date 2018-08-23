using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MavenMetaDataFixer
{
    public class MetaDataFixer
    {
        private readonly string pathRoot;

        public MetaDataFixer(string pathRoot)
        {
            this.pathRoot = pathRoot;
        }

        public void UpdateMetaData()
        {
            RecurseMetaData(pathRoot);
        }

        private IEnumerable<string> GetVersionsForMetaData(string dataFile, string artifactId)
        {
            var parent = Path.GetDirectoryName(dataFile);

            return Directory.EnumerateDirectories(parent).Where(dir => File.Exists(Path.Combine(dir, $"{artifactId}-{Path.GetFileName(dir)}.pom")))
                                                         .Select(dir => Path.GetFileName(dir));
        }

        private void UpdateSpecificMetaData(string dataFile)
        {
            XDocument doc = XDocument.Parse(File.ReadAllText(dataFile));
            var artifactId = doc.Descendants().Where(x => x.Name.LocalName == "artifactId").Select(x => x.Value).First();
            var versionsBlock = doc.Descendants().Where(x => x.Name.LocalName == "versioning").First().Descendants().Where(x => x.Name.LocalName == "versions").First();
            versionsBlock.RemoveNodes();
            var newVersions = GetVersionsForMetaData(dataFile, artifactId).OrderBy(x => NuGet.SemanticVersion.Parse(x)).Select(x => new XElement("version", x));
            foreach (var v in newVersions)
            {
                versionsBlock.Add(v);
            }
            File.WriteAllText(dataFile, doc.ToString());
        }

        private void RecurseMetaData(string root)
        {
            string metaDataFile = Path.Combine(root, "maven-metadata.xml");
            if (File.Exists(metaDataFile))
            {
                UpdateSpecificMetaData(metaDataFile);
            }
            foreach(var dir in Directory.EnumerateDirectories(root))
            {
                RecurseMetaData(dir);
            }
        }
    }
}
