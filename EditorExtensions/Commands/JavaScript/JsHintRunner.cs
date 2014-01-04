using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnvDTE;
using MadsKristensen.EditorExtensions.Commands.JavaScript;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class JsHintRunner : IDisposable
    {
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

        public JsHintRunner(string fileName)
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

            if (ShouldIgnore(_fileName))
            {
                // In case this file was added to JSHintIgnore after it was opened, clear the existing errors.
                _provider.Tasks.Clear();
                return;
            }

            EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Running JSHint...";

            CompilerResult result = await new JsHintCompiler().Compile(_fileName, GetConfigurationFilePath());

            // Hack to select result from Error: 
            // See https://github.com/madskristensen/WebEssentials2013/issues/392#issuecomment-31566419
            ReadResult(result.Errors);
        }

        private string GetConfigurationFilePath()
        {
            return FindLocalSettings(_fileName) ?? GetGlobalSettings();
        }

        private static string GetGlobalSettings()
        {
            string jsHintRc = Path.Combine(Settings.GetWebEssentialsSettingsFolder(), ".jshintrc");

            if (!File.Exists(jsHintRc))
                File.Copy(Path.Combine(Path.GetDirectoryName(typeof(LessCompiler).Assembly.Location), @"Resources\Settings Backup\.jshintrc")
                        , jsHintRc);

            return jsHintRc;
        }

        private static string FindLocalSettings(string sourcePath)
        {
            string dir = Path.GetDirectoryName(sourcePath);

            while (!File.Exists(Path.Combine(dir, ".jshintrc")))
            {
                dir = Path.GetDirectoryName(dir);
                if (String.IsNullOrEmpty(dir))
                    return null;
            }

            return Path.Combine(dir, ".jshintrc");
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

        private static FileCache<JsHintIgnoreList> ignoreListCache = new FileCache<JsHintIgnoreList>(JsHintIgnoreList.Load);

        public static bool ShouldIgnore(string file)
        {
            if (!Path.GetExtension(file).Equals(".js", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".debug.js", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".intellisense.js", StringComparison.OrdinalIgnoreCase) ||
                file.Contains("-vsdoc.js") ||
                !File.Exists(file))
            {
                return true;
            }

            ProjectItem item = ProjectHelpers.GetProjectItem(file);

            if (item == null)
                return true;

            // Ignore files nested under other files such as bundle or TypeScript output
            ProjectItem parent = item.Collection.Parent as ProjectItem;
            if (parent != null && !Directory.Exists(parent.FileNames[1]) || File.Exists(item.FileNames[1] + ".bundle"))
                return true;

            var ignoreFile = FindLocalIgnore(file);

            if (ignoreFile != null && ignoreListCache.Get(ignoreFile).IsIgnored(file))
                return true;

            string name = Path.GetFileName(file);
            return MustIgnore(name);
        }

        private static string FindLocalIgnore(string sourcePath)
        {
            string dir = Path.GetDirectoryName(sourcePath);

            while (!File.Exists(Path.Combine(dir, ".jshintignore")))
            {
                dir = Path.GetDirectoryName(dir);
                if (String.IsNullOrEmpty(dir))
                    return null;
            }

            return Path.Combine(dir, ".jshintignore");
        }


        private static bool MustIgnore(string name)
        {
            if (_builtInIgnoreRegex.IsMatch(name))
                return true;

            if (_parsedUserIgnoreList != WESettings.GetString(WESettings.Keys.JsHint_ignoreFiles))
                ParseUserIgnoreList();

            return _userIgnoreRegexes.Any(r => r.IsMatch(name));
        }

        static string _parsedUserIgnoreList;
        static IReadOnlyCollection<Regex> _userIgnoreRegexes = new Regex[0];
        static void ParseUserIgnoreList()
        {
            _parsedUserIgnoreList = WESettings.GetString(WESettings.Keys.JsHint_ignoreFiles);
            _userIgnoreRegexes = _parsedUserIgnoreList.Split(';')
                .Select(s =>
            {
                s = s.Trim();
                if (s.Length == 0) return null;
                try
                {
                    return new Regex(s, RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    Logger.ShowMessage("Skipping invalid regex '" + s + "' in JSHint ignore patterns.\nPlease fix that in Web Essentials Options.\n\n" + ex.Message);
                    return null;
                }
            }).Where(r => r != null).ToList();
        }

        private static Regex _builtInIgnoreRegex = new Regex("(" + String.Join(")|(", new[] {
            @"_references\.js",
            @"amplify\.js",
            @"angular\.js",
            @"backbone\.js",
            @"bootstrap\.js",
            @"dojo\.js",
            @"ember\.js",
            @"ext-core\.js",
            @"handlebars.*",
            @"highlight\.js",
            @"history\.js",
            @"jquery-([0-9\.]+)\.js",
            @"jquery.blockui.*",
            @"jquery.validate.*",
            @"jquery.unobtrusive.*",
            @"jquery-ui-([0-9\.]+)\.js",
            @"json2\.js",
            @"knockout-([0-9\.]+)\.js",
            @"MicrosoftAjax([a-z]+)\.js",
            @"modernizr-([0-9\.]+)\.js",
            @"mustache.*",
            @"prototype\.js ",
            @"qunit-([0-9a-z\.]+)\.js",
            @"require\.js",
            @"respond\.js",
            @"sammy\.js",
            @"scriptaculous\.js ",
            @"swfobject\.js",
            @"underscore\.js",
            @"webfont\.js",
            @"yepnope\.js",
            @"zepto\.js",
        }) + ")", RegexOptions.IgnoreCase);

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
                Logger.Log("Error reading JSHint result");
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
                ErrorCategory = GetOutputLocation(),
                Category = TaskCategory.Html,
                Document = error.FileName,
                Priority = TaskPriority.Low,
                Text = error.Message,
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