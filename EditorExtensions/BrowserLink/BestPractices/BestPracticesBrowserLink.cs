using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Web.BrowserLink;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(BrowserLinkExtensionFactory))]
    [BrowserLinkFactoryName("BestPractices")] // Not needed in final version of VS2013
    public class BestPracticesFactory : BrowserLinkExtensionFactory
    {
        private static BestPractices _extension;

        public override BrowserLinkExtension CreateInstance(BrowserLinkConnection connection)
        {
            // Instantiate the extension as a singleton
            if (_extension == null)
            {
                _extension = new BestPractices();
            }

            return _extension;
        }

        public override string Script
        {
            get
            {
                using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.BestPractices.BestPracticesBrowserLink.js"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    public class BestPractices : BrowserLinkExtension
    {   
        private Dictionary<string, ErrorTask> _tasks = new Dictionary<string, ErrorTask>();
        private RulesFactory _factory = new RulesFactory();
        
        public ErrorListProvider _errorList;
        public BrowserLinkConnection _connection;

        public BestPractices()
        {
            _errorList = new ErrorListProvider(EditorExtensionsPackage.Instance);
            _errorList.ProviderName = "Browser Link Extension";
            _errorList.ProviderGuid = new Guid("5BA8BB0D-D518-45ae-966C-864C536454F2");
        }

        public override void OnConnected(BrowserLinkConnection connection)
        {
            _connection = connection;
        }
        
        [BrowserLinkCallback]
        public void Error(string id, bool success, string data)
        {
            IRule rule = _factory.FindRule(id, data, this);

            if (rule != null)
            {
                CreateTask(id, rule, success);
                _errorList.Tasks.Clear();

                if (_tasks.Count > 0)
                {
                    _errorList.SuspendRefresh();

                    foreach (string key in _tasks.Keys)
                    {
                        _errorList.Tasks.Add(_tasks[key]);
                    }

                    _errorList.ResumeRefresh();
                    _errorList.Show();
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
                    ErrorCategory = TaskErrorCategory.Message,
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
            IVsHierarchy HierarchyItem;
            IVsSolution solution = EditorExtensionsPackage.GetGlobalService<IVsSolution>(typeof(SVsSolution));

            if (solution != null)
            {
                int flag = solution.GetProjectOfUniqueName(_connection.Project.FullName, out HierarchyItem);

                if (0 == flag)
                {
                    task.HierarchyItem = HierarchyItem;
                }
            }
        }
    }
}