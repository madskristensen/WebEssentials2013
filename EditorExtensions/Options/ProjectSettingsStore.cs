using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Keys = MadsKristensen.EditorExtensions.WESettings.Keys;

namespace MadsKristensen.EditorExtensions
{
    internal class Settings
    {
        public const string _fileName = "WE-settings.xml";
        public const string _solutionFolder = "Solution Items";

        private static SortedDictionary<string, object> _cache = DefaultSettings();
        private static bool _inProgress;
        private static object _syncFileRoot = new object();
        private static object _syncCacheRoot = new object();

        public Settings()
        {
            UpdateCache();
        }

        public static bool SolutionSettingsExist
        {
            get { return File.Exists(GetSolutionFilePath()); }
        }

        public static float Version { get; private set; }

        public static object GetValue(string propertyName)
        {
            lock (_syncCacheRoot)
            {
                if (_cache.ContainsKey(propertyName))
                    return _cache[propertyName];
            }

            return null;
        }

        public static void SetValue(string propertyName, object value)
        {
            lock (_syncCacheRoot)
            {
                string v = value.ToString().ToLowerInvariant();
                _cache[propertyName] = v;
            }
        }

        public static void Save(string file = null)
        {
            //_dispatcher.BeginInvoke(new Action(() =>
            //{
            Task.Run(() =>
            {
                SaveToDisk(file);
                UpdateStatusBar("updated");
            });

            //}), DispatcherPriority.ApplicationIdle, null);
        }

        internal static void CreateSolutionSettings()
        {
            string path = GetSolutionFilePath();

            if (!File.Exists(path))
            {
                lock (_syncFileRoot)
                {
                    File.WriteAllText(path, string.Empty);
                }

                Save(path);

                Solution2 solution = EditorExtensionsPackage.DTE.Solution as Solution2;
                Project project = solution.Projects
                                    .OfType<Project>()
                                    .FirstOrDefault(p => p.Name.Equals(_solutionFolder, StringComparison.OrdinalIgnoreCase));

                if (project == null)
                {
                    project = solution.AddSolutionFolder(_solutionFolder);
                }

                project.ProjectItems.AddFromFile(path);
                //EditorExtensionsPackage.DTE.ItemOperations.OpenFile(path);
                UpdateStatusBar("applied");
            }
        }

        public static void UpdateCache()
        {
            try
            {
                string path = GetFilePath();

                if (File.Exists(path))
                {
                    XmlDocument doc = LoadXmlDocument(path);

                    if (doc != null)
                    {

                        XmlNode settingsNode = doc.SelectSingleNode("webessentials/settings");

                        if (settingsNode != null)
                        {
                            XmlAttribute versionAttr = settingsNode.Attributes["version"];
                            if (versionAttr != null)
                            {
                                float version;

                                if (float.TryParse(versionAttr.InnerText, out version))
                                {
                                    Version = version;
                                }
                            }

                            lock (_syncCacheRoot)
                            {
                                _cache.Clear();

                                foreach (XmlNode node in settingsNode.ChildNodes)
                                {
                                    _cache[node.Name] = node.InnerText;
                                }
                            }

                            OnUpdated();
                        }
                    }
                }
            }
            catch
            { }
        }

        private static void SaveToDisk(string file)
        {
            if (!_inProgress)
            {
                _inProgress = true;
                string path = file ?? GetFilePath();

                lock (_syncFileRoot)
                {
                    string xml = GenerateXml();

                    ProjectHelpers.CheckOutFileFromSourceControl(path);
                    File.WriteAllText(path, xml);
                }

                _inProgress = false;
            }
        }

        private static string GenerateXml()
        {
            StringBuilder sb = new StringBuilder();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("webessentials");
                writer.WriteAttributeString("version", "1.9");

                writer.WriteStartElement("settings");

                lock (_syncCacheRoot)
                {
                    foreach (string property in _cache.Keys)
                    {
                        string value = _cache[property].ToString();
                        writer.WriteElementString(property, value);
                    }
                }

                writer.WriteEndElement();// settings
                writer.WriteEndElement();// webessentials
            }

            sb.Replace(Encoding.Unicode.WebName, Encoding.UTF8.WebName);

            return sb.ToString();
        }

        private static XmlDocument LoadXmlDocument(string path)
        {
            try
            {
                lock (_syncFileRoot)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);
                    return doc;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string GetFilePath()
        {
            string path = GetSolutionFilePath();

            if (!File.Exists(path))
            {
                path = GetUserFilePath();
            }

            return path;
        }

        public static string GetSolutionFilePath()
        {
            EnvDTE.Solution solution = EditorExtensionsPackage.DTE.Solution;

            if (solution == null || string.IsNullOrEmpty(solution.FullName))
                return null;

            return Path.Combine(Path.GetDirectoryName(solution.FullName), _fileName);
        }

        private static string GetUserFilePath()
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string folder = Path.Combine(user, "Web Essentials");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return Path.Combine(folder, _fileName);
        }

        private static SortedDictionary<string, object> DefaultSettings()
        {
            var dic = new SortedDictionary<string, object>();

            // MISC
            dic.Add(Keys.EnableMustache, true);
            dic.Add(Keys.EnableJavascriptRegions, true);

            // LESS
            dic.Add(Keys.GenerateCssFileFromLess, true);
            dic.Add(Keys.ShowLessPreviewWindow, true);
            dic.Add(Keys.LessMinify, true);

            // SCSS
            dic.Add(Keys.GenerateCssFileFromScss, true);
            dic.Add(Keys.ShowScssPreviewWindow, true);
            dic.Add(Keys.ScssMinify, true);

            // CoffeeScript
            dic.Add(Keys.GenerateJsFileFromCoffeeScript, true);
            dic.Add(Keys.ShowCoffeeScriptPreviewWindow, true);

            // CSS
            dic.Add(Keys.CssErrorLocation, (int)Keys.ErrorLocation.Messages);
            dic.Add(Keys.SyncVendorValues, true);
            dic.Add(Keys.EnableCssSelectorHighligting, true);
            dic.Add(Keys.ShowUnsupported, true);

            //JSHint
            dic.Add(Keys.EnableJsHint, true);
            dic.Add(Keys.JsHintErrorLocation, (int)Keys.FullErrorLocation.Messages);
            dic.Add(Keys.JsHint_bitwise, true);
            dic.Add(Keys.JsHint_browser, true);
            dic.Add(Keys.JsHint_devel, true);
            dic.Add(Keys.JsHint_eqeqeq, true);
            dic.Add(Keys.JsHint_expr, true);
            dic.Add(Keys.JsHint_debug, true);
            dic.Add(Keys.JsHint_jquery, true);
            dic.Add(Keys.JsHint_laxbreak, true);
            dic.Add(Keys.JsHint_laxcomma, true);
            dic.Add(Keys.JsHint_maxerr, 50);
            dic.Add(Keys.JsHint_regexdash, true);
            dic.Add(Keys.JsHint_smarttabs, true);
            dic.Add(Keys.JsHint_undef, true);
            dic.Add(Keys.JsHint_unused, true);

            // MISC
            dic.Add(Keys.ShowBrowserTooltip, true);
            dic.Add(Keys.WrapCoffeeScriptClosure, true);

            // TypeScript
            //dic.Add(Keys.ShowTypeScriptPreviewWindow, true);
            //dic.Add(Keys.GenerateJsFileFromTypeScript, true);
            //dic.Add(Keys.TypeScriptAddGeneratedFilesToProject, true);

            // Minification
            dic.Add(Keys.EnableCssMinification, true);
            dic.Add(Keys.EnableJsMinification, true);

            // Minification
            dic.Add(Keys.CoffeeScriptMinify, true);
            //dic.Add(Keys.TypeScriptMinify, true);

            dic.Add(Keys.GenerateJavaScriptSourceMaps, true);
            dic.Add(Keys.EnableHtmlZenCoding, true);

            dic.Add(Keys.JavaScriptAutoCloseBraces, true);
            dic.Add(Keys.JavaScriptOutlining, true);

            return dic;
        }

        public static event EventHandler Updated;

        private static void OnUpdated()
        {
            if (Updated != null)
            {
                Updated(null, EventArgs.Empty);
            }
        }

        public static void UpdateStatusBar(string action)
        {
            try
            {
                if (SolutionSettingsExist)
                {
                    EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Solution settings " + action;
                }
                else
                {
                    EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Global settings " + action;
                }
            }
            catch
            {
                Logger.Log("Error updating status bar");
            }
        }
    }
}
