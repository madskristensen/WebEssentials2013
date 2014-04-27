using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;
using EnvDTE;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Helpers;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Threading = System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    [ContentType("node.js")]
    [ContentType("htmlx")]
    [ContentType("XML")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class BundleFilesMenu : IWpfTextViewCreationListener
    {
        private static DTE2 _dte;
        private OleMenuCommandService _mcs;
        public const string _ext = ".bundle";
        private static string[] _ignoreFolders = new[] { "app_data", "bin", "obj", "pkg" };

        public BundleFilesMenu()
        {
            // Used by the IWpfTextViewCreationListener
            _dte = EditorExtensionsPackage.DTE;
        }

        public BundleFilesMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            ITextDocument document;
            textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document);

            if (document != null)
                document.FileActionOccurred += document_FileActionOccurred;
        }

        public async static Threading.Task BindAllBundlesAssets(string path)
        {
            if (path == null)
                return;

            foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(path), "*.bundle", SearchOption.AllDirectories))
                await ObserveBundleFileObjects(file);
        }

        private async static Threading.Task ObserveBundleFileObjects(string file)
        {
            XmlDocument doc = await GetXmlDocument(file);

            if (doc == null)
                return;

            XmlNode bundleNode = doc.SelectSingleNode("//bundle");

            if (bundleNode == null)
                return;

            XmlNodeList nodes = doc.SelectNodes("//file");

            foreach (XmlNode node in nodes)
                await new BundleFileWatcher().AttachFileObserverEvent(ProjectHelpers.ToAbsoluteFilePath(node.InnerText, file));
        }

        private async void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (!new[] { ".bundle", ".css", ".js" }.Any(x => Path.GetExtension(e.FilePath).Equals(x, StringComparison.OrdinalIgnoreCase)) ||
                e.FileActionType != FileActionTypes.ContentLoadedFromDisk && e.FileActionType != FileActionTypes.ContentSavedToDisk)
                return;

            string file = null;

            if (e.FilePath.EndsWith(_ext, StringComparison.OrdinalIgnoreCase))
            {
                await ObserveBundleFileObjects(e.FilePath);
                file = e.FilePath;
            }

            await System.Threading.Tasks.Task.Run(async () =>
            {
                await UpdateBundles(file, true);
            });
        }

        public async static Threading.Task UpdateBundles(string changedFile, bool isBuild)
        {
            if (!string.IsNullOrEmpty(changedFile))
            {
                await UpdateBundle(changedFile, isBuild);
                return;
            }

            foreach (Project project in ProjectHelpers.GetAllProjects())
            {
                if (project.ProjectItems.Count == 0)
                    continue;

                string folder = ProjectHelpers.GetRootFolder(project);
                await UpdateBundle(folder, isBuild);
            }
        }

        private async static Threading.Task UpdateBundle(string changedFile, bool isBuild)
        {
            string absolutePath = ProjectHelpers.FixAbsolutePath(changedFile);
            bool isDir = Directory.Exists(changedFile);
            string dir = isDir ? changedFile : ProjectHelpers.GetProjectFolder(changedFile);

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;

            foreach (string file in Directory.EnumerateFiles(dir, "*" + _ext, SearchOption.AllDirectories))
            {
                if (_ignoreFolders.Any(p => file.Contains("\\" + p + "\\")))
                    continue;

                XmlDocument doc = await GetXmlDocument(file);

                if (doc == null)
                    continue;

                XmlNode bundleNode = doc.SelectSingleNode("//bundle");

                if (bundleNode == null)
                    continue;

                string baseDir = Path.GetDirectoryName(file);

                if ((changedFile != null && doc.SelectNodes("//file").Cast<XmlNode>().Any(x =>
                     absolutePath == ProjectHelpers.ToAbsoluteFilePath(x.InnerText, dir, baseDir))) ||
                    (isBuild && bundleNode.Attributes["runOnBuild"] != null &&
                     bundleNode.Attributes["runOnBuild"].InnerText == "true"))
                    WriteBundleFile(file, doc).DoNotWait("reading " + file + " file");
            }
        }

        private async static Task<XmlDocument> GetXmlDocument(string filePath)
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml((await FileHelpers.ReadAllTextRetry(filePath)).Trim());
                return xmlDocument;
            }
            catch (Exception)
            {
                Logger.Log(Path.GetFileName(filePath) + " is not a valid Web Essentials bundle file. Ignoring file.");
                return null;
            }
        }

        public void SetupCommands()
        {
            // TODO: Replace with single class that takes ContentType
            CommandID commandCss = new CommandID(CommandGuids.guidBundleCmdSet, (int)CommandId.BundleCss);
            OleMenuCommand menuCommandCss = new OleMenuCommand(async (s, e) => await CreateBundlefile(".css"), commandCss);
            menuCommandCss.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".css"); };
            _mcs.AddCommand(menuCommandCss);

            CommandID commandJs = new CommandID(CommandGuids.guidBundleCmdSet, (int)CommandId.BundleJs);
            OleMenuCommand menuCommandJs = new OleMenuCommand(async (s, e) => await CreateBundlefile(".js"), commandJs);
            menuCommandJs.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".js"); };
            _mcs.AddCommand(menuCommandJs);

            CommandID commandHtml = new CommandID(CommandGuids.guidBundleCmdSet, (int)CommandId.BundleHtml);
            OleMenuCommand menuCommandHtml = new OleMenuCommand(async (s, e) => await CreateBundlefile(".html"), commandHtml);
            menuCommandHtml.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".html"); };
            _mcs.AddCommand(menuCommandHtml);
        }

        private static void BeforeQueryStatus(object sender, string extension)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            menuCommand.Enabled = GetSelectedItems(extension).Count() > 1;
        }

        private static IEnumerable<ProjectItem> GetSelectedItems(string extension)
        {
            return ProjectHelpers.GetSelectedItems().Where(p => Path.GetExtension(p.FileNames[1]) == extension);
        }

        private async static Threading.Task CreateBundlefile(string extension)
        {
            var items = GetSelectedItems(extension);

            if (items.Count() == 0)
                return;

            StringBuilder sb = new StringBuilder();

            foreach (ProjectItem item in items)
            {
                if (!File.Exists(item.FileNames[1]))
                    continue;

                string content = await FileHelpers.ReadAllTextRetry(item.FileNames[1]);
                sb.AppendLine(content);
            }

            ProjectItem firstItem = items.SkipWhile(x => x.FileNames[1] != null).FirstOrDefault();

            if (firstItem == null || string.IsNullOrEmpty(firstItem.FileNames[1]))
                return;

            string dir = Path.GetDirectoryName(firstItem.FileNames[1]);

            if (!Directory.Exists(dir))
                return;

            string bundleFile = Microsoft.VisualBasic.Interaction.InputBox("Specify the name of the bundle", "Web Essentials", "bundle1");

            if (string.IsNullOrEmpty(bundleFile))
                return;

            if (!bundleFile.EndsWith(extension + _ext, StringComparison.OrdinalIgnoreCase))
                bundleFile += extension + _ext;

            string bundlePath = Path.Combine(dir, bundleFile);

            if (File.Exists(bundlePath))
            {
                Logger.ShowMessage("The bundle file already exists.");
                return;
            }

            await Dispatcher.CurrentDispatcher.BeginInvoke(new Action(async () =>
                  await WriteFile(bundlePath, items, Path.ChangeExtension(bundleFile, null))), // Remove the final ".bundle" extension.
                  DispatcherPriority.ApplicationIdle, null);
        }

        private async static Threading.Task WriteFile(string filePath, IEnumerable<ProjectItem> files, string output)
        {
            string projectRoot = ProjectHelpers.GetProjectFolder(files.ElementAt(0).FileNames[1]);
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("bundle");
                writer.WriteAttributeString("minify", "true");
                writer.WriteAttributeString("runOnBuild", "true");
                writer.WriteAttributeString("output", output);
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", null, "http://vswebessentials.com/schemas/v1/bundle.xsd");
                writer.WriteComment("The order of the <file> elements determines the order of the file contents when bundled.");

                foreach (ProjectItem item in files)
                {
                    string relative = item.IsLink() ? item.FileNames[1] : "/" + FileHelpers.RelativePath(projectRoot, item.FileNames[1]);
                    writer.WriteElementString("file", relative);
                }

                writer.WriteEndElement();
            }

            sb.Replace(Encoding.Unicode.WebName, Encoding.UTF8.WebName);

            ProjectHelpers.CheckOutFileFromSourceControl(filePath);
            await FileHelpers.WriteAllTextRetry(filePath, sb.ToString());
            ProjectHelpers.AddFileToActiveProject(filePath, "None");

            _dte.ItemOperations.OpenFile(filePath);

            //TODO: Use XLINQ
            XmlDocument doc = await GetXmlDocument(filePath);

            if (doc == null)
                return;

            await Dispatcher.CurrentDispatcher.BeginInvoke(
                  new Action(() => WriteBundleFile(filePath, doc).DoNotWait("writing " + filePath + "file")), DispatcherPriority.ApplicationIdle, null);
        }

        private async static Threading.Task WriteBundleFile(string bundleFilePath, XmlDocument doc)
        {
            XmlNode bundleNode = doc.SelectSingleNode("//bundle");

            if (bundleNode == null)
                return;

            XmlNode outputAttr = bundleNode.Attributes["output"];

            if (outputAttr != null && (outputAttr.InnerText.Contains("/") || outputAttr.InnerText.Contains("\\")))
            {
                Logger.ShowMessage(String.Format(CultureInfo.CurrentCulture, "The 'output' attribute should contain a file name without a path; '{0}' is not valid", outputAttr.InnerText));
                return;
            }

            Dictionary<string, string> files = new Dictionary<string, string>();

            // filePath must end in ".targetExtension.bundle"
            string extension = Path.GetExtension(Path.GetFileNameWithoutExtension(bundleFilePath));

            if (string.IsNullOrEmpty(extension))
            {
                Logger.Log("Skipping bundle file " + bundleFilePath + " without extension.  Bundle files must end with the output extension, followed by '.bundle'.");
                return;
            }

            XmlNodeList nodes = doc.SelectNodes("//file");

            foreach (XmlNode node in nodes)
            {
                string absolute;

                if (node.InnerText.Contains(":\\"))
                {
                    absolute = node.InnerText;
                }
                else
                {
                    absolute = ProjectHelpers.ToAbsoluteFilePath(node.InnerText, bundleFilePath);
                }

                if (File.Exists(absolute))
                {
                    if (!files.ContainsKey(absolute))
                        files.Add(absolute, node.InnerText);
                }
                else
                {
                    _dte.ItemOperations.OpenFile(bundleFilePath);
                    Logger.ShowMessage(String.Format(CultureInfo.CurrentCulture, "Bundle error: The file '{0}' doesn't exist", node.InnerText));

                    return;
                }
            }

            string bundleSourcePath = outputAttr != null ? Path.Combine(Path.GetDirectoryName(bundleFilePath), outputAttr.InnerText) : bundleFilePath.Replace(_ext, string.Empty);
            StringBuilder sb = new StringBuilder();

            foreach (string file in files.Keys)
            {
                //if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
                //{
                //    sb.AppendLine("/*#source " + files[file] + " */");
                //}
                if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) && WESettings.Instance.JavaScript.GenerateSourceMaps)
                {
                    sb.AppendLine("///#source 1 1 " + files[file]);
                }

                if (!File.Exists(file))
                    continue;

                await new BundleFileWatcher().AttachFileObserverEvent(file);

                var source = await FileHelpers.ReadAllTextRetry(file);

                if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
                {
                    // If the bundle is in the same folder as the CSS,
                    // or if does not have URLs, no need to normalize.
                    if (Path.GetDirectoryName(file) != Path.GetDirectoryName(bundleSourcePath) &&
                        source.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0 &&
                        WESettings.Instance.Css.AdjustRelativePaths)
                        source = CssUrlNormalizer.NormalizeUrls(
                            tree: new CssParser().Parse(source, true),
                            targetFile: bundleSourcePath,
                            oldBasePath: file
                        );
                }

                sb.AppendLine(source);
            }

            bool bundleChanged = !File.Exists(bundleSourcePath) || await FileHelpers.ReadAllTextRetry(bundleSourcePath) != sb.ToString();

            if (bundleChanged)
            {
                ProjectHelpers.CheckOutFileFromSourceControl(bundleSourcePath);
                await FileHelpers.WriteAllTextRetry(bundleSourcePath, sb.ToString());
                Logger.Log("Web Essentials: Updated bundle: " + Path.GetFileName(bundleSourcePath));
            }

            ProjectHelpers.AddFileToProject(bundleFilePath, bundleSourcePath);

            if (bundleNode.Attributes["minify"] != null && bundleNode.Attributes["minify"].InnerText == "true")
                await WriteMinFile(bundleSourcePath, extension, bundleChanged);
        }

        private async static Threading.Task WriteMinFile(string bundleSourcePath, string extension, bool bundleChanged)
        {
            string minPath = Path.ChangeExtension(bundleSourcePath, ".min" + Path.GetExtension(bundleSourcePath));

            // If the bundle didn't change, don't re-minify, unless the user just enabled minification.
            if (!bundleChanged && File.Exists(minPath))
                return;

            var fers = WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>().Value;
            var contentType = fers.GetContentTypeForExtension(extension);
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            var minifier = Mef.GetImport<IFileMinifier>(contentType);
            bool changed = await minifier.MinifyFile(bundleSourcePath, minPath);

            if (settings.GzipMinifiedFiles && (changed || !File.Exists(minPath + ".gzip")))
                FileHelpers.GzipFile(minPath);
        }

        private class BundleFileWatcher
        {
            private FileSystemWatcher _watcher;
            private readonly AsyncReaderWriterLock rwLock = new AsyncReaderWriterLock();
            private static HashSet<string> _watchedFiles = new HashSet<string>();

            internal async Threading.Task AttachFileObserverEvent(string fileName)
            {
                if (!File.Exists(fileName))
                    return;

                fileName = Path.GetFullPath(fileName);

                using (await rwLock.ReadLockAsync())
                {
                    if (_watchedFiles.Contains(fileName))
                        return;
                }

                using (await rwLock.WriteLockAsync())
                {
                    _watchedFiles.Add(fileName);
                }

                _watcher = new FileSystemWatcher();

                _watcher.Path = Path.GetDirectoryName(fileName);
                _watcher.Filter = Path.GetFileName(fileName);
                _watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size;

                _watcher.Changed += async (s, e) => await UpdateBundles(null, true);

                _watcher.EnableRaisingEvents = true;
            }
        }
    }
}