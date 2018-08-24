using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WPILibInstaller
{
    public class ExtractionIgnores
    {
        private CheckBox checkBox;
        private string folder;
        private bool forceIgnore;
        private bool fullDir;

        public ExtractionIgnores(CheckBox cb, string folder, bool fullDir)
        {
            this.checkBox = cb;
            this.folder = folder;
            forceIgnore = false;
            this.fullDir = fullDir;
        }

        public ExtractionIgnores(string folder, bool fullDir)
        {
            this.folder = folder;
            this.forceIgnore = true;
            this.fullDir = fullDir;
        }

        public void AddToIgnoreList(List<string> list)
        {
            if (forceIgnore || !checkBox.Checked)
            {
                if (fullDir)
                {
                    list.Add(folder);
                }
                else
                {
                    list.Add(folder + "/");
                }
                
            }
        }
    }
}
