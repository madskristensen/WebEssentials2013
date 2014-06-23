using System;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    public class RobotsTxtRule : IRule
    {
        private BestPractices _extension;

        public RobotsTxtRule(BestPractices extension)
        {
            _extension = extension;
        }

        public string Message
        {
            get { return "SEO: The file 'robots.txt' is missing in the root of the website."; }
        }

        public string Question
        {
            get { return "Do you want to add a robots.txt?"; }
        }

        public TaskErrorCategory Category
        {
            get { return TaskErrorCategory.Warning; }
        }

        public void Navigate(object sender, EventArgs e)
        {
            ErrorTask task = (ErrorTask)sender;

            if (MessageBox.Show(Question, "Web Essentials", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Project project = _extension.Connection.Project;
                string folder = project.Properties.Item("FullPath").Value.ToString();
                string path = Path.Combine(folder, "robots.txt");

                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine("User-agent: *");
                    writer.WriteLine("Disallow: /admin");
                }

                WebEssentialsPackage.DTE.ItemOperations.OpenFile(path);
                project.ProjectItems.AddFromFile(path);
                _extension.ErrorList.Tasks.Remove(task);
            }
        }
    }
}
