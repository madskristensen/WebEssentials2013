﻿using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    internal class JsHintRunner : IDisposable
    {
        private ErrorListProvider _provider;
        private static Dictionary<string, ErrorListProvider> _providers = new Dictionary<string, ErrorListProvider>();
        private string _fileName;
        private bool _isDisposed;

        static JsHintRunner()
        {
            EditorExtensionsPackage.DTE.Events.SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
        }

        static void SolutionEvents_AfterClosing()
        {
            Reset();
            EditorExtensionsPackage.DTE.Events.SolutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
        }

        public JsHintRunner(string fileName)
        {
            _fileName = fileName;

            if (_providers.ContainsKey(fileName))
            {
                _provider = _providers[fileName];
            }
            else
            {
                _provider = new ErrorListProvider(EditorExtensionsPackage.Instance);
                _providers.Add(fileName, _provider);
            }
        }

        public void RunCompiler()
        {
            if (!_isDisposed && !ShouldIgnore(_fileName))
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Running JSHint...";
                JsHintCompiler lint = new JsHintCompiler(Dispatcher.CurrentDispatcher);

                System.Threading.Tasks.Task.Run(() =>
                {
                    using (StreamReader reader = new StreamReader(_fileName))
                    {
                        string content = reader.ReadToEnd();

                        lint.Completed += LintCompletedHandler;
                        lint.Compile(content, _fileName);
                    }
                });
            }
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

        public static bool ShouldIgnore(string file)
        {
            if (!Path.GetExtension(file).Equals(".js", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".min.js") ||
                file.EndsWith(".debug.js") ||
                file.EndsWith(".intellisense.js") ||
                !File.Exists(file) ||
                EditorExtensionsPackage.DTE.Solution.FindProjectItem(file) == null)
            {
                return true;
            }

            string name = Path.GetFileName(file);

            foreach (string regex in _ignoreList)
            {
                if (Regex.IsMatch(name, regex, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> _ignoreList = new List<string>()
        {
            @"jquery-([0-9\.]+)\.js",
            @"jquery-ui-([0-9\.]+)\.js",
            @"knockout-([0-9\.]+)\.js",
            @"modernizr-([0-9\.]+)\.js",
            @"backbone\.js",
            @"angular\.js",
            @"amplify\.js",
            @"dojo\.js",
            @"ember\.js",
            @"handlebars-([0-9a-z\.]+)\.js",
            @"mustache\.js",
            @"underscore\.js",
            @"yepnope\.js",
            @"ext-core\.js",
            @"highlight\.js",
            @"history\.js",
            @"require\.js",
            @"sammy\.js",
            @"json2\.js",
            @"_references\.js",
            @"MicrosoftAjax([a-z]+)\.js",
            @"scriptaculous\.js ",
            @"prototype\.js ",
            @"qunit-([0-9a-z\.]+)\.js",
            @"swfobject\.js",
            @"bootstrap\.js",
            @"webfont\.js",
            @"zepto\.js",
        };

        private void LintCompletedHandler(object sender, CompilerEventArgs e)
        {
            using (JsHintCompiler lint = (JsHintCompiler)sender)
            {
                if (!_isDisposed)
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        ReadResult(e);
                    });
                }

                lint.Completed -= LintCompletedHandler;
            }

            EditorExtensionsPackage.DTE.StatusBar.Clear();
        }

        private void ReadResult(CompilerEventArgs e)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Result[] results = serializer.Deserialize<Result[]>(e.Result);

                _provider.SuspendRefresh();
                _provider.Tasks.Clear();

                foreach (Result error in results.Where(r => r != null))
                {
                    ErrorTask task = CreateTask(e.State, error);
                    _provider.Tasks.Add(task);
                }

                _provider.ResumeRefresh();
            }
            catch
            {
                Logger.Log("Error reading JSHint result");
            }
        }

        private ErrorTask CreateTask(string data, Result error)
        {
            ErrorTask task = new ErrorTask()
            {
                Line = error.line,
                Column = error.character,
                ErrorCategory = GetOutputLocation(),
                Category = TaskCategory.Html,
                Document = data,
                Priority = TaskPriority.Low,
                Text = GetErrorMessage(error),
            };

            task.AddHierarchyItem();

            task.Navigate += task_Navigate;
            return task;
        }

        private static TaskErrorCategory GetOutputLocation()
        {
            var location = (WESettings.Keys.FullErrorLocation)WESettings.GetInt(WESettings.Keys.JsHintErrorLocation);

            if (location == WESettings.Keys.FullErrorLocation.Errors)
                return TaskErrorCategory.Error;

            if (location == WESettings.Keys.FullErrorLocation.Warnings)
                return TaskErrorCategory.Warning;

            return TaskErrorCategory.Message;
        }

        private string GetErrorMessage(Result error)
        {
            string raw = error.raw;
            if (raw == "Missing radix parameter.")
                raw = "When using the parseInt function, remember to specify the radix parameter. Example: parseInt('3', 10)";

            return "JSHint (r10): " + raw.Replace("{a}", error.a).Replace("{b}", error.b);
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