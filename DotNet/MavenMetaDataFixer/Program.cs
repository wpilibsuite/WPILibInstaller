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
            // Get location of EXE
            var exeFullPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            // Get directory EXE is in
            var exePath = Path.GetDirectoryName(exeFullPath);

            // Update data down from exe path
            MetaDataFixer fixer = new MetaDataFixer(exePath);
            fixer.UpdateMetaData();

            // If silent, return early, otherwise show message
            if (args.Contains("silent"))
            {
                return;
            }
            MessageBox.Show("MetaData Successfully Updated");
        }


    }
}
