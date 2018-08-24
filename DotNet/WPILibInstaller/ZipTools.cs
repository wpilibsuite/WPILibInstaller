using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WPILibInstaller
{
    public static class ZipTools
    {
        public static async Task<bool> UnzipToDirectory(string fromFile, string intoPath, ProgressBar progress, bool skipFirstDirectory = false, bool doDeleteDirectory = true)
        {

            try
            {
                Directory.Delete(intoPath, true);
            }
            catch (IOException)
            {
                
            }

            using (FileStream fs = new FileStream(fromFile, FileMode.Open))
            using (ZipFile zf = new ZipFile(fs))
            {
                zf.IsStreamOwner = false;

                double totalCount = zf.Count;
                long currentCount = 0;

                foreach(ZipEntry entry in zf)
                {
                    double percentage = (currentCount / totalCount) * 100;
                    currentCount++;
                    progress.Value = (int)percentage;

                    if (!entry.IsFile)
                    {
                        continue;
                    }

                    var entryName = entry.Name;

                    Stream zipStream = zf.GetInputStream(entry);

                    // Manipulate the output filename here as desired.

                    if (skipFirstDirectory)
                    {
                        string[] split = entryName.Split(new[] { '/' }, 2);
                        if (split.Length > 1)
                        {
                            entryName = split[1];
                        }
                    }
                    
                    String fullZipToPath = Path.Combine(intoPath, entryName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                    {
                        try
                        {
                            Directory.CreateDirectory(directoryName);
                        }
                        catch (IOException)
                        {

                        }
                    }

                    using (FileStream writer = File.Create(fullZipToPath))
                    {
                        await zipStream.CopyToAsync(writer);
                    }

                }
            }

            return true;
        }
    }
}
