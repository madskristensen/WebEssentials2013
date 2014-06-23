using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    public class Description : IRule
    {
        private string _file;
        private BestPractices _extension;

        public Description(string file, BestPractices extension)
        {
            _file = file;
            _extension = extension;
        }

        public string Message
        {
            get { return "SEO: Specify the page description using a <meta> tag. Double-click to fix"; }
        }

        public string Question
        {
            get { return "Do you want to insert a description <meta> tag?"; }
        }

        public TaskErrorCategory Category
        {
            get { return TaskErrorCategory.Warning; }
        }

        public async void Navigate(object sender, EventArgs e)
        {
            ErrorTask task = (ErrorTask)sender;

            if (MessageBox.Show(Question, "Web Essentials", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!File.Exists(_file))
                    return;

                string html = await FileHelpers.ReadAllTextRetry(_file);
                int index = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);

                if (index > -1)
                {
                    WebEssentialsPackage.DTE.ItemOperations.OpenFile(_file);
                    _extension.ErrorList.Tasks.Remove(task);
                    AddMetaTag(index);

                    return;
                }
            }
        }

        private static void AddMetaTag(int index)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                var view = ProjectHelpers.GetCurentTextView();
                var buffer = view.TextBuffer;

                using (WebEssentialsPackage.UndoContext("Adding <meta> description"))
                {
                    buffer.Insert(index, "<meta name=\"description\" content=\"The description of my page\" />" + Environment.NewLine);
                    view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, index + 34 + 26));
                    view.Selection.Select(new SnapshotSpan(buffer.CurrentSnapshot, 34 + index, 26), false);
                    WebEssentialsPackage.ExecuteCommand("Edit.FormatSelection");
                }

                WebEssentialsPackage.DTE.ActiveDocument.Save();
                view.ViewScroller.EnsureSpanVisible(new SnapshotSpan(buffer.CurrentSnapshot, index, 1), EnsureSpanVisibleOptions.AlwaysCenter);

            }), DispatcherPriority.ApplicationIdle, null);
        }
    }
}
