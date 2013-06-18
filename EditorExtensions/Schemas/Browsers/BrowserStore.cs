using EnvDTE80;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Schemas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MadsKristensen.EditorExtensions
{
    class BrowserStore
    {
        private const string _fileName = "WE-browsers.xml";
        private static FileSystemWatcher _watcher;
        private static List<string> _browsers = new List<string>();

        public static List<string> Browsers
        {
            get
            {
                string path = GetSolutionFilePath();
                
                if (_browsers.Count == 0 && File.Exists(path))
                {
                    ParseXml(path);
                    InitializeWatcher(path);
                }

                return _browsers;
            }
        }

        private static void ParseXml(string path)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                foreach (XmlNode node in doc.SelectNodes("//browser"))
                {
                    _browsers.Add(node.InnerText.ToUpperInvariant());
                }
            }
            catch
            {
                _browsers.Clear();
            }
        }

        public static void SaveBrowsers(IEnumerable<string> browsers)
        {
            Solution2 solution = EditorExtensionsPackage.DTE.Solution as Solution2;
            EnvDTE.Project project = solution.Projects
                                .OfType<EnvDTE.Project>()
                                .FirstOrDefault(p => p.Name.Equals(Settings._solutionFolder, StringComparison.OrdinalIgnoreCase));

            if (project == null)
            {
                project = solution.AddSolutionFolder(Settings._solutionFolder);
            }

            string path = GetSolutionFilePath();

            using (XmlWriter writer = XmlWriter.Create(path, new XmlWriterSettings() { Indent = true }))
            {
                writer.WriteStartElement("browsers");

                foreach (string browser in browsers)
                {
                    writer.WriteElementString("browser", browser);
                }

                writer.WriteEndElement();
            }

            project.ProjectItems.AddFromFile(path);
            CssSchemaManager.SchemaManager.ReloadSchemas();
        }

        public static string GetSolutionFilePath()
        {
            EnvDTE.Solution solution = EditorExtensionsPackage.DTE.Solution;

            if (solution == null || string.IsNullOrEmpty(solution.FullName))
                return null;

            return Path.Combine(Path.GetDirectoryName(solution.FullName), _fileName);
        }

        private static void InitializeWatcher(string filePath)
        {
            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher();
                _watcher.Path = Path.GetDirectoryName(filePath);
                _watcher.Filter = Path.GetFileName(filePath);
                _watcher.EnableRaisingEvents = true;
                _watcher.Created += delegate { _browsers.Clear(); };
                _watcher.Changed += delegate { _browsers.Clear(); };
                _watcher.Deleted += delegate { _browsers.Clear(); };
                _watcher.Renamed += delegate { _browsers.Clear(); };
            }
        }
    }
}
