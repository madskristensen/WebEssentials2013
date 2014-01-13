using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class TsLintRunner : IDisposable
    {
        // TODO: Unify with JsHintRunner
        private readonly ErrorListProvider _provider;
        private readonly static Dictionary<string, ErrorListProvider> _providers = InitializeResources();
        private readonly string _fileName;
        private bool _isDisposed;

        static Dictionary<string, ErrorListProvider> InitializeResources()
        {
            EditorExtensionsPackage.DTE.Events.SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
            return new Dictionary<string, ErrorListProvider>();
        }

        static void SolutionEvents_AfterClosing()
        {
            Reset();
            EditorExtensionsPackage.DTE.Events.SolutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
        }

        public TsLintRunner(string fileName)
        {
            _fileName = fileName;

            if (!_providers.TryGetValue(fileName, out _provider))
            {
                _provider = new ErrorListProvider(EditorExtensionsPackage.Instance);
                _providers.Add(fileName, _provider);
            }
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

        public async void RunCompiler()
        {
            if (_isDisposed)
                return;

            EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Running TSLint...";

            CompilerResult result = await new TsLintCompiler().Check(_fileName);

            EditorExtensionsPackage.DTE.StatusBar.Clear();

            // Hack to select result from Error: 
            // See https://github.com/madskristensen/WebEssentials2013/issues/392#issuecomment-31566419
            ReadResult(result.Errors);
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

        private void ReadResult(IEnumerable<CompilerError> results)
        {
            if (results == null)
                return;

            try
            {
                _provider.SuspendRefresh();
                _provider.Tasks.Clear();

                foreach (CompilerError error in results.Where(r => r != null))
                {
                    ErrorTask task = CreateTask(error);
                    _provider.Tasks.Add(task);
                }

                _provider.ResumeRefresh();
            }
            catch
            {
                Logger.Log("Error reading TSLint result");
            }
            finally
            {
                Clean();
            }
        }

        private ErrorTask CreateTask(CompilerError error)
        {
            ErrorTask task = new ErrorTask()
            {
                Line = error.Line,
                Column = error.Column,
                ErrorCategory = WESettings.Instance.TypeScript.LintResultLocation,
                Category = TaskCategory.Html,
                Document = error.FileName,
                Priority = TaskPriority.Low,
                Text = error.Message,
            };

            task.AddHierarchyItem();

            task.Navigate += task_Navigate;
            return task;
        }

        private void task_Navigate(object sender, EventArgs e)
        {
            Task task = sender as Task;

            _provider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindPrimary));

            if (task.Column > 0)
            {
                var doc = (TextDocument)EditorExtensionsPackage.DTE.ActiveDocument.Object("textdocument");
                doc.Selection.MoveToDisplayColumn(task.Line, task.Column);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_providers.ContainsKey(_fileName))
                {
                    _providers.Remove(_fileName);
                }

                _provider.Tasks.Clear();
                _provider.Dispose();
            }

            _isDisposed = true;
        }
    }
}