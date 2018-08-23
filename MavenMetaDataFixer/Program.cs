using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MavenMetaDataFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            var exeFullPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            var exePath = Path.GetDirectoryName(exeFullPath);

            var pth = Path.GetFileName(exePath);

            ;

            pth = @"C:\Users\thadh\releases\maven\development";

            MetaDataFixer fixer = new MetaDataFixer(pth);
            fixer.UpdateMetaData();

            if (args.Contains("silent"))
            {
                return;
            }
            MessageBox.Show("MetaData Successfully Updated");
        }


    }
}
