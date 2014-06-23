using System;
using System.Globalization;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class RuleExtensions
    {
        public static Task ProduceErrorListTask(this IStylingRule rule, TaskErrorCategory category, Project project, string format)
        {
            var item = ResolveVsHierarchyItem(project.UniqueName);
            var task = new ErrorTask
            {
                Document = rule.File,
                Line = rule.Line - 1,
                Column = rule.Column,
                ErrorCategory = category,
                Category = TaskCategory.Html,
                Text = string.Format(CultureInfo.CurrentCulture, format, project.Name, rule.DisplaySelectorName, rule.File, rule.Line, rule.Column),
                HierarchyItem = item
            };

            task.Navigate += NavigateToItem;

            return task;
        }

        private static void NavigateToItem(object sender, EventArgs e)
        {
            var task = (ErrorTask)sender;
            var doc = task.Document;

            Window window;

            try
            {
                window = WebEssentialsPackage.DTE.ItemOperations.OpenFile(doc);
            }
            catch (ArgumentException)
            {
                //If the file has been deleted and the error is still there, swallow the exception (as OpenFile will fail) and return
                return;
            }

            var selection = (TextSelection)window.Selection;

            selection.MoveTo(task.Line + 1, task.Column + 1);
        }

        private static IVsHierarchy ResolveVsHierarchyItem(string projectName)
        {
            IVsHierarchy hierarchyItem = null;
            var solution = WebEssentialsPackage.GetGlobalService<IVsSolution>(typeof(SVsSolution));

            if (solution != null)
            {
                ErrorHandler.ThrowOnFailure(solution.GetProjectOfUniqueName(projectName, out hierarchyItem));
            }

            return hierarchyItem;
        }
    }
}
