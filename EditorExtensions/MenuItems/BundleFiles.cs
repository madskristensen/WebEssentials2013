using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Xml;
using EnvDTE;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Helpers;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    [ContentType("htmlx")]
    [ContentType("XML")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class BundleFilesMenu : IWpfTextViewCreationListener
    {
        private static DTE2 _dte;
        private OleMenuCommandService _mcs;
        public const string _ext = ".bundle";
        private static string[] _ignoreFolders = new[] { "app_data", "bin", "obj" };

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
            {
                document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        private void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                string file = e.FilePath.EndsWith(_ext, StringComparison.OrdinalIgnoreCase) ? e.FilePath : null;

                System.Threading.Tasks.Task.Run(() =>
                {
                    UpdateBundles(file, true);
                });
            }
        }

        public static void UpdateBundles(string changedFile, bool isBuild)
        {
            if (!string.IsNullOrEmpty(changedFile))
            {
                UpdateBundle(changedFile, isBuild);
                return;
            }
            foreach (Project project in ProjectHelpers.GetAllProjects())
            {
                if (project.ProjectItems.Count > 0)
                {
                    string folder = ProjectHelpers.GetRootFolder(project);
                    UpdateBundle(folder, isBuild);
                }
            }
        }

        private static void UpdateBundle(string changedFile, bool isBuild)
        {
            bool isDir = Directory.Exists(changedFile);

            string dir = isDir ? changedFile : ProjectHelpers.GetProjectFolder(changedFile);

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;

            foreach (string file in Directory.GetFiles(dir, "*" + _ext, SearchOption.AllDirectories))
            {
                if (_ignoreFolders.Any(p => file.IndexOf("\\" + p + "\\", StringComparison.OrdinalIgnoreCase) > -1))
                    continue;

                XmlDocument doc = GetXmlDocument(file);
                var bundleFileDir = Path.GetDirectoryName(file);
                bool enabled = false;

                if (doc != null)
                {
                    XmlNode bundleNode = doc.SelectSingleNode("//bundle");
                    if (bundleNode == null)
                        continue;

                    XmlNodeList nodes = doc.SelectNodes("//file");
                    foreach (XmlNode node in nodes)
                    {
                        string relative = node.InnerText;
                        string absolute = ProjectHelpers.ToAbsoluteFilePath(relative, dir, bundleFileDir);

                        if (changedFile != null && absolute.Equals(ProjectHelpers.FixAbsolutePath(changedFile), StringComparison.OrdinalIgnoreCase))
                        {
                            enabled = true;
                            break;
                        }
                    }

                    if (isBuild && bundleNode.Attributes["runOnBuild"] != null && bundleNode.Attributes["runOnBuild"].InnerText == "true")
                    {
                        enabled = true;
                    }

                    if (enabled)
                    {
                        WriteBundleFile(file, doc);
                    }
                }
            }
        }

        private static XmlDocument GetXmlDocument(string filePath)
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(File.ReadAllText(filePath).Trim());
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
            OleMenuCommand menuCommandCss = new OleMenuCommand((s, e) => CreateBundlefile(".css"), commandCss);
            menuCommandCss.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".css"); };
            _mcs.AddCommand(menuCommandCss);

            CommandID commandJs = new CommandID(CommandGuids.guidBundleCmdSet, (int)CommandId.BundleJs);
            OleMenuCommand menuCommandJs = new OleMenuCommand((s, e) => CreateBundlefile(".js"), commandJs);
            menuCommandJs.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".js"); };
            _mcs.AddCommand(menuCommandJs);

            CommandID commandHtml = new CommandID(CommandGuids.guidBundleCmdSet, (int)CommandId.BundleHtml);
            OleMenuCommand menuCommandHtml = new OleMenuCommand((s, e) => CreateBundlefile(".html"), commandHtml);
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

        private static void CreateBundlefile(string extension)
        {
            StringBuilder sb = new StringBuilder();
            string firstFile = null;
            var items = GetSelectedItems(extension);

            foreach (ProjectItem item in items)
            {
                if (string.IsNullOrEmpty(firstFile))
                    firstFile = item.FileNames[1];

                if (File.Exists(item.FileNames[1]))
                {
                    string content = File.ReadAllText(item.FileNames[1]);
                    sb.AppendLine(content);
                }
            }

            if (firstFile != null)
            {
                string dir = Path.GetDirectoryName(firstFile);

                if (Directory.Exists(dir))
                {
                    string bundleFile = Microsoft.VisualBasic.Interaction.InputBox("Specify the name of the bundle", "Web Essentials", "bundle1");

                    if (string.IsNullOrEmpty(bundleFile))
                        return;

                    if (!bundleFile.EndsWith(extension + _ext, StringComparison.OrdinalIgnoreCase))
                        bundleFile += extension + _ext;

                    string bundlePath = Path.Combine(dir, bundleFile);

                    if (File.Exists(bundlePath))
                    {
                        Logger.ShowMessage("The bundle file already exists.");
                    }
                    else
                    {
                        Dispatcher.CurrentDispatcher.BeginInvoke(         // Remove the final ".bundle" extension.
                            new Action(() => WriteFile(bundlePath, items, Path.ChangeExtension(bundleFile, null))),
                        DispatcherPriority.ApplicationIdle, null);
                    }
                }
            }
        }
        private static void WriteFile(string filePath, IEnumerable<ProjectItem> files, string output)
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
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            ProjectHelpers.AddFileToActiveProject(filePath, "None");

            _dte.ItemOperations.OpenFile(filePath);

            //TODO: Use XLINQ
            XmlDocument doc = GetXmlDocument(filePath);

            if (doc != null)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => WriteBundleFile(filePath, doc)), DispatcherPriority.ApplicationIdle, null);
            }
        }

        private static void WriteBundleFile(string bundleFilePath, XmlDocument doc)
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

                var source = File.ReadAllText(file);
                if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
                {
                    // If the bundle is in the same folder as the CSS,
                    // or if does not have URLs, no need to normalize.
                    if (Path.GetDirectoryName(file) != Path.GetDirectoryName(bundleSourcePath)
                     && source.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0
                     && WESettings.Instance.Css.AdjustRelativePaths)
                        source = CssUrlNormalizer.NormalizeUrls(
                            tree: new CssParser().Parse(source, true),
                            targetFile: bundleSourcePath,
                            oldBasePath: file
                        );
                }
                sb.AppendLine(source);
            }

            bool bundleChanged = !File.Exists(bundleSourcePath) || File.ReadAllText(bundleSourcePath) != sb.ToString();
            if (bundleChanged)
            {
                ProjectHelpers.CheckOutFileFromSourceControl(bundleSourcePath);
                File.WriteAllText(bundleSourcePath, sb.ToString(), new UTF8Encoding(true));
                Logger.Log("Web Essentials: Updated bundle: " + Path.GetFileName(bundleSourcePath));
            }

            ProjectHelpers.AddFileToProject(bundleFilePath, bundleSourcePath);

            if (bundleNode.Attributes["minify"] != null && bundleNode.Attributes["minify"].InnerText == "true")
            {
                WriteMinFile(bundleSourcePath, extension, bundleChanged);
            }
        }

        private static void WriteMinFile(string bundleSourcePath, string extension, bool bundleChanged)
        {
            string minPath = Path.ChangeExtension(bundleSourcePath, ".min" + Path.GetExtension(bundleSourcePath));

            // If the bundle didn't change, don't re-minify, unless the user just enabled minification.
            if (!bundleChanged && File.Exists(minPath))
                return;

            var fers = WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>().Value;
            var contentType = fers.GetContentTypeForExtension(extension);
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            var minifier = Mef.GetImport<IFileMinifier>(contentType);
            bool changed = minifier.MinifyFile(bundleSourcePath, minPath);

            if (settings.GzipMinifiedFiles && (changed || !File.Exists(minPath + ".gzip")))
                FileHelpers.GzipFile(minPath);
        }
    }
}