using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EnvDTE;
using MadsKristensen.EditorExtensions.JavaScript;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>Runs a linting tool on a single file and displays the results in the error list.</summary>
    internal class LintReporter : IDisposable
    {
        private bool _isDisposed;
        protected readonly ErrorListProvider _provider;
        private readonly ILintCompiler _compiler;

        public ILinterSettings Settings { get; private set; }
        public string FileName { get; private set; }

        #region Static provider management
        private readonly static Dictionary<string, ErrorListProvider> _providers = InitializeResources();

        static Dictionary<string, ErrorListProvider> InitializeResources()
        {
            WebEssentialsPackage.DTE.Events.SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
            return new Dictionary<string, ErrorListProvider>();
        }

        static void SolutionEvents_AfterClosing()
        {
            Reset();
            WebEssentialsPackage.DTE.Events.SolutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
        }

        public static void Reset()
        {
            foreach (string key in _providers.Keys)
            {
                _providers[key].Tasks.Clear();
                _providers[key].Dispose();
            }

            _providers.Clear();
        }
        private static void Clean()
        {
            var nonExisting = _providers.Keys.FirstOrDefault(k => !File.Exists(k));
            if (!string.IsNullOrEmpty(nonExisting))
            {
                _providers[nonExisting].Tasks.Clear();
                _providers[nonExisting] = null;
                _providers.Remove(nonExisting);
            }
        }
        #endregion

        public LintReporter(ILintCompiler compiler, ILinterSettings settings, string fileName)
        {
            Settings = settings;
            FileName = fileName;
            _compiler = compiler;

            if (!_providers.TryGetValue(fileName, out _provider))
            {
                _provider = new ErrorListProvider(WebEssentialsPackage.Instance);
                _providers.Add(fileName, _provider);
            }
        }


        public virtual async Task RunLinterAsync()
        {
            if (_isDisposed)
                return;

            WebEssentialsPackage.DTE.StatusBar.Text = "Web Essentials: Running " + _compiler.ServiceName + "...";

            CompilerResult result = await _compiler.CheckAsync(FileName);

            WebEssentialsPackage.DTE.StatusBar.Clear();

            // Hack to select result from Error: 
            // See https://github.com/madskristensen/WebEssentials2013/issues/392#issuecomment-31566419
            ReadResult(result.Errors);
        }


        private void ReadResult(IEnumerable<CompilerError> results)
        {
            if (results == null)
                return;

            try
            {
                _provider.SuspendRefresh();

                for (int i = _provider.Tasks.Count - 1; i > -1; i--)
                {
                    var task = _provider.Tasks[i];

                    if (task.Text.StartsWith(_compiler.ServiceName, StringComparison.OrdinalIgnoreCase))
                        _provider.Tasks.Remove(task);
                }

                // HACK: stop JSCS (and any other) from output more than 500 items, hope JSCS can do this
                int errorLimit = 500;
                int resultCount = results.Count();
                int count = 0;
                foreach (CompilerError error in results.Where(r => r != null))
                {
                    ErrorTask task;

                    // JSHint has build in limit up to 500
                    // the 501 one is "too many" one, no need to replace it with this hack
                    if (++count > errorLimit &&
                        !(_compiler is JsHintCompiler && resultCount == 501))
                    {
                        task = CreateTask(error,
                            string.Format(CultureInfo.InvariantCulture, "{0}: Too many errors. Only shows {1} errors from {2}.",
                                _compiler.ServiceName, errorLimit, results.Count()));
                        _provider.Tasks.Add(task);
                        break;
                    }

                    task = CreateTask(error);
                    _provider.Tasks.Add(task);
                }
            }
            finally
            {
                _provider.ResumeRefresh();
                Clean();
            }
        }

        private ErrorTask CreateTask(CompilerError error, string message = null)
        {
            ErrorTask task = new ErrorTask()
            {
                Line = error.Line,
                Column = error.Column,
                ErrorCategory = Settings.LintResultLocation,
                Category = TaskCategory.Html,
                Document = error.FileName,
                Priority = TaskPriority.Low,
                Text = message ?? error.Message,
            };

            task.AddHierarchyItem();

            task.Navigate += task_Navigate;
            return task;
        }

        private void task_Navigate(object sender, EventArgs e)
        {
            ErrorTask task = sender as ErrorTask;

            _provider.Navigate(task, new Guid(Constants.vsViewKindPrimary));

            if (task.Column > 0)
            {
                var doc = (TextDocument)WebEssentialsPackage.DTE.ActiveDocument.Object("textdocument");
                doc.Selection.MoveToDisplayColumn(task.Line, task.Column);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _providers.Remove(FileName);
                _provider.Tasks.Clear();
                _provider.Dispose();
            }

            _isDisposed = true;
        }
    }
}