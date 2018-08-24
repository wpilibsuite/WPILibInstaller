using SharedCode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPILibInstaller
{
    public static class GradleSetup
    {
        public static async Task SetupGradle(FullConfig config, string extractFolder)
        {
            string gradleZipLoc = Path.Combine(extractFolder, "installUtils", config.Gradle.ZipName);

            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string toFolder = Path.Combine(userFolder, ".gradle", config.Gradle.ExtractLocation, Path.GetFileNameWithoutExtension(config.Gradle.ZipName), config.Gradle.Hash);
            string toFile = Path.Combine(toFolder, config.Gradle.ZipName);
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    Directory.CreateDirectory(toFolder);
                }
                catch (IOException)
                {

                }
                File.Copy(gradleZipLoc, toFile, true);
            });
        }
    }
}
