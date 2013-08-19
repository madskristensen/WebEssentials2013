using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MadsKristensen.EditorExtensions
{
    public class Favicon : IRule
    {
        public string Message
        {
            get { return "Usability: The file 'favicon.ico' is missing in the root of the website."; }
        }

        public string Question
        {
            get { return "Do you want to add a favicon.ico?"; }
        }

        public void Navigate(object sender, EventArgs e)
        {
            ErrorTask task = sender as ErrorTask;

            if (MessageBox.Show(Question, "Demo", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string assembly = Assembly.GetExecutingAssembly().Location;
                string extFolder = Path.GetDirectoryName(assembly).ToLowerInvariant();
                string extFile = Path.Combine(extFolder, "resources\\favicon.ico");

                Project p = ArteryExtensionPackage.GetActiveProject();
                string folder = p.Properties.Item("FullPath").Value.ToString();
                string path = Path.Combine(folder, "favicon.ico");

                File.Copy(extFile, path);

                p.ProjectItems.AddFromFile(path);
                ArteryExtensionPackage.ErrorList.Tasks.Remove(task);
            }
        }
    }
}
