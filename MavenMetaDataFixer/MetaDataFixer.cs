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

        private struct Artifact : IEquatable<Artifact>
        {
            public string GroupId { get; }
            public string ArtifactId { get; }

            public Artifact(string groupId, string artifactId)
            {
                GroupId = groupId;
                ArtifactId = artifactId;
            }

            public override bool Equals(object obj)
            {
                return obj is Artifact && Equals((Artifact)obj);
            }

            public bool Equals(Artifact other)
            {
                return GroupId == other.GroupId &&
                       ArtifactId == other.ArtifactId;
            }

            public override int GetHashCode()
            {
                var hashCode = -2042083241;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(GroupId);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ArtifactId);
                return hashCode;
            }

            public static bool operator ==(Artifact artifact1, Artifact artifact2)
            {
                return artifact1.Equals(artifact2);
            }

            public static bool operator !=(Artifact artifact1, Artifact artifact2)
            {
                return !(artifact1 == artifact2);
            }
        }

        private readonly Dictionary<Artifact, List<string>> artifactStore = new Dictionary<Artifact, List<string>>();
        

        public MetaDataFixer(string pathRoot)
        {
            this.pathRoot = pathRoot;
        }

        public void UpdateMetaData()
        {
            RecurseMetaData(pathRoot);
            foreach (var artifact in artifactStore)
            {
                var dataFile = Path.Combine(pathRoot, artifact.Key.GroupId.Replace('.', Path.DirectorySeparatorChar), artifact.Key.ArtifactId, "maven-metadata.xml");
                XDocument doc = new XDocument(new XDeclaration("1.0", "UTF8", null));
                XElement metadata = new XElement("metadata");
                metadata.Add(new XElement("groupId", artifact.Key.GroupId));
                metadata.Add(new XElement("artifactId", artifact.Key.ArtifactId));
                XElement versioning = new XElement("versioning");
                artifact.Value.Sort();
                versioning.Add(new XElement("release", artifact.Value[artifact.Value.Count - 1]));
                XElement versions = new XElement("versions");
                foreach (var version in artifact.Value)
                {
                    versions.Add(new XElement("version", version));
                }
                versioning.Add(versions);
                var now = DateTime.UtcNow;
                var nowStr = now.ToString("yyyyMMddHHmmss");
                versioning.Add(new XElement("lastUpdated", nowStr));
                metadata.Add(versioning);
                doc.Add(metadata);
                var newData = doc.Declaration.ToString() + '\n' + doc.ToString().Replace("\r\n", "\n") + '\n';
                File.WriteAllText(dataFile, newData);
                FixHashes(dataFile, newData);
            }
        }

        private void RecurseMetaData(string root)
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.pom"))
            {
                using (FileStream fs = new FileStream(file, FileMode.Open))
                {
                    XElement doc = XElement.Load(fs);
                    var ns = doc.Name.Namespace;

                    string groupId = "";
                    var groupIdNode = doc.Element(ns + "groupId");
                    if (groupIdNode == null)
                    {
                        groupId = doc.Element(ns + "parent").Element(ns + "groupId").Value;
                    }
                    else
                    {
                        groupId = groupIdNode.Value;
                    }
                    var artifactId = doc.Element(ns + "artifactId").Value;
                    string version = "";
                    var versionNode = doc.Element(ns + "version");
                    if (versionNode == null)
                    {
                        version = doc.Element(ns + "parent").Element(ns + "version").Value;
                    }
                    else
                    {
                        version = versionNode.Value;
                    }
                    var key = new Artifact(groupId, artifactId);
                    if (artifactStore.TryGetValue(key, out var versions))
                    {
                        versions.Add(version);
                    }
                    else
                    {
                        List<string> data = new List<string>
                        {
                            version
                        };
                        artifactStore.Add(key, data);
                    }
                    ;
                }
                ;
            }
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                RecurseMetaData(dir);
            }
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
    }
}
