using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class BestPracticesFactory : IBrowserLinkExtensionFactory
    {
        private static BestPractices _extension;

        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            // Instantiate the extension as a singleton
            if (_extension == null)
            {
                _extension = new BestPractices();
            }

            return _extension;
        }

        public string GetScript()
        {
            using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.BestPractices.BestPracticesBrowserLink.js"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class BestPractices : BrowserLinkExtension
    {
        private Dictionary<string, ErrorTask> _tasks = new Dictionary<string, ErrorTask>();

        public ErrorListProvider ErrorList { get; private set; }
        public BrowserLinkConnection Connection { get; private set; }

        public BestPractices()
        {
            ErrorList = new ErrorListProvider(EditorExtensionsPackage.Instance);
            ErrorList.ProviderName = "Browser Link Extension";
            ErrorList.ProviderGuid = new Guid("5BA8BB0D-D518-45ae-966C-864C536454F2");
        }

        public override void OnConnected(BrowserLinkConnection connection)
        {
            Connection = connection;
        }

        [BrowserLinkCallback]
        public void Error(string id, bool success, string data)
        {
            IRule rule = RulesFactory.FindRule(id, data, this);

            if (rule != null)
            {
                CreateTask(id, rule, success);
                ErrorList.Tasks.Clear();

                if (_tasks.Count > 0)
                {
                    ErrorList.SuspendRefresh();

                    foreach (string key in _tasks.Keys)
                    {
                        ErrorList.Tasks.Add(_tasks[key]);
                    }

                    ErrorList.ResumeRefresh();
                }
            }
        }

        private void CreateTask(string id, IRule rule, bool success)
        {
            if (_tasks.ContainsKey(id) && success)
            {
                _tasks.Remove(id);
            }
            else if (!success)
            {
                ErrorTask task = new ErrorTask()
                {
                    Document = id,
                    Line = 1,
                    Column = 1,
                    ErrorCategory = rule.Category,
                    Category = TaskCategory.Html,
                    Text = rule.Message,
                };

                AddHierarchyItem(task);

                task.Navigate += rule.Navigate;
                _tasks[id] = task;
            }
        }

        public void AddHierarchyItem(ErrorTask task)
        {
            if (task == null || Connection == null || Connection.Project == null || string.IsNullOrEmpty(Connection.Project.FullName))
                return;

            IVsHierarchy HierarchyItem;
            IVsSolution solution = EditorExtensionsPackage.GetGlobalService<IVsSolution>(typeof(SVsSolution));

            if (solution != null)
            {
                int flag = solution.GetProjectOfUniqueName(Connection.Project.FullName, out HierarchyItem);

                if (0 == flag)
                {
                    task.HierarchyItem = HierarchyItem;
                }
            }
        }
    }
}