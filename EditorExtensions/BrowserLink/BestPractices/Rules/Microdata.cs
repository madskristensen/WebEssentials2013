using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    public class Microdata : IRule
    {
        public string Message
        {
            get { return "SEO: Use HTML5 microdata to add semantic meaning to the website."; }
        }

        public string Question
        {
            get { return "Do you want to browse to a tutorial?"; }
        }

        public TaskErrorCategory Category
        {
            get { return TaskErrorCategory.Message; }
        }

        public void Navigate(object sender, EventArgs e)
        {
            if (MessageBox.Show(Question, "Web Essentials", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Process.Start("http://www.seomoves.org/blog/build/html5-microdata-2711/");
            }
        }
    }
}