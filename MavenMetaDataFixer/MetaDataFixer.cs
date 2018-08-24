using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
            var dirs = Directory.EnumerateDirectories(pathRoot, "*", SearchOption.AllDirectories);
            Parallel.ForEach(dirs, dir =>
            {
                string metaDataFile = Path.Combine(dir, "maven-metadata.xml");
                if (File.Exists(metaDataFile))
                {
                    UpdateSpecificMetaData(metaDataFile);
                }
            });
        }

        private void FixHashes(string dataFile, string text)
        {
            var txtBytes = Encoding.UTF8.GetBytes(text);
            StringBuilder sb = new StringBuilder(50);
            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(txtBytes);
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                var md5Str = sb.ToString();
                File.WriteAllText(dataFile + ".md5", md5Str);
            }
            using (var sha1 = SHA1.Create())
            {
                var hashBytes = sha1.ComputeHash(txtBytes);
                sb.Clear();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                var sha1Str = sb.ToString();
                File.WriteAllText(dataFile + ".sha1", sha1Str);
            }
        }

        private IEnumerable<string> GetVersionsForMetaData(string dataFile, string artifactId)
        {
            var parent = Path.GetDirectoryName(dataFile);

            return Directory.EnumerateDirectories(parent).Where(dir => File.Exists(Path.Combine(dir, $"{artifactId}-{Path.GetFileName(dir)}.pom")))
                                                         .Select(dir => Path.GetFileName(dir));
        }

        private void UpdateSpecificMetaData(string dataFile)
        {
            var origData = File.ReadAllText(dataFile);
            XDocument doc = XDocument.Parse(origData);
            var artifactId = doc.Descendants().Where(x => x.Name.LocalName == "artifactId").Select(x => x.Value).First();
            var versioningBlock = doc.Descendants().Where(x => x.Name.LocalName == "versioning").First();
            var versionsBlock = versioningBlock.Descendants().Where(x => x.Name.LocalName == "versions").First();
            versionsBlock.RemoveNodes();
            var newVersions = GetVersionsForMetaData(dataFile, artifactId).OrderBy(x => x).Select(x => new XElement("version", x));
            foreach (var v in newVersions)
            {
                versionsBlock.Add(v);
            }
            var now = DateTime.UtcNow;
            var nowStr = now.ToString("yyyyMMddHHmmss");
            versioningBlock.Descendants().Where(x => x.Name.LocalName == "lastUpdated").First().Value = nowStr;
            var newData = doc.Declaration.ToString() + '\n' + doc.ToString().Replace("\r\n", "\n") + '\n';
            File.WriteAllText(dataFile, newData);
            FixHashes(dataFile, newData);
        }
    }
}
