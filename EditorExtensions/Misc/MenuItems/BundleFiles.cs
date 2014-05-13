using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    [ContentType("node.js")]
    [ContentType("htmlx")]
    [ContentType("XML")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class BundleFilesMenu
    {
        private static DTE2 _dte;
        private OleMenuCommandService _mcs;
        public const string _ext = ".bundle";
        private IEnumerable<string> _files;
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

        public void SetupCommands()
        {
            // TODO: Replace with single class that takes ContentType
            CommandID commandCss = new CommandID(CommandGuids.guidBundleCmdSet, (int)CommandId.BundleCss);
            OleMenuCommand menuCommandCss = new OleMenuCommand(async (s, e) => await MakeBundleAsync(".css"), commandCss);
            menuCommandCss.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".css"); };
            _mcs.AddCommand(menuCommandCss);

            CommandID commandJs = new CommandID(CommandGuids.guidBundleCmdSet, (int)CommandId.BundleJs);
            OleMenuCommand menuCommandJs = new OleMenuCommand(async (s, e) => await MakeBundleAsync(".js"), commandJs);
            menuCommandJs.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".js"); };
            _mcs.AddCommand(menuCommandJs);

            CommandID commandHtml = new CommandID(CommandGuids.guidBundleCmdSet, (int)CommandId.BundleHtml);
            OleMenuCommand menuCommandHtml = new OleMenuCommand(async (s, e) => await MakeBundleAsync(".html"), commandHtml);
            menuCommandHtml.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".html"); };
            _mcs.AddCommand(menuCommandHtml);
        }

        private void BeforeQueryStatus(object sender, string extension)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            _files = ProjectHelpers.GetSelectedFilePaths();

            menuCommand.Enabled = _files.All(file => extension == Path.GetExtension(file)) &&
                                  _files.Count() > 1;
        }

        private bool GetFileName(out string fileName, string extension)
        {
            fileName = "MyBundle";

            string directory = Path.GetDirectoryName(_files.First());

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = fileName;
                dialog.DefaultExt = extension + ".bundle";
                dialog.Filter = "Bundle File|*.bundle";
                dialog.InitialDirectory = directory;

                if (dialog.ShowDialog() != DialogResult.OK)
                    return false;

                fileName = dialog.FileName;

                if (!fileName.EndsWith(extension + ".bundle", StringComparison.OrdinalIgnoreCase))
                    fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.ChangeExtension(Path.GetFileNameWithoutExtension(fileName), extension) + ".bundle");
            }

            //Check for colliding file names (remove bundle from name before passing in).
            string collidedFile = FileHelpers.GetFileCollisions(Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName)));
            if (collidedFile == null)
            {
                return true;
            }

            if (MessageBox.Show("The following existing file conflicts with a file that would be generated:\r\n'" + collidedFile + "'.\r\n\r\nWould you like to retry with a different name or cancel?", "File name issue", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation) == DialogResult.Retry)
            {
                return GetFileName(out fileName, extension);
            }

            _dte.StatusBar.Text = "Bundle generation canceled.";
            return false;
        }

        public static async Task UpdateAllBundlesAsync(bool isBuild = false)
        {
            foreach (Project project in ProjectHelpers.GetAllProjects())
            {
                if (project.ProjectItems.Count == 0)
                    continue;

                string folder = ProjectHelpers.GetRootFolder(project);

                foreach (string file in Directory.EnumerateFiles(folder, "*" + _ext, SearchOption.AllDirectories))
                {
                    if (ProjectHelpers.GetProjectItem(file) == null)
                        continue;

                    await new BundleFilesMenu().UpdateBundleAsync(file, isBuild);
                }
            }
        }

        private async Task UpdateBundleAsync(string file, bool isBuild = false)
        {
            string extension = null;

            if (!file.EndsWith(_ext, StringComparison.OrdinalIgnoreCase))
                return;

            extension = Path.GetExtension(Path.GetFileNameWithoutExtension(file));

            if (string.IsNullOrEmpty(extension))
                return;

            await UpdateBundleAsync(file, extension);
        }

        private async Task UpdateBundleAsync(string bundleFileName, string extension)
        {
            if (_ignoreFolders.Any(p => bundleFileName.Contains("\\" + p + "\\")))
                return;

            try
            {
                BundleDocument doc = BundleDocument.FromFile(bundleFileName);
                await GenerateAsync(doc, extension, true);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Log(ex.Message);
                MessageBox.Show("The file '" + Path.GetFileName(ex.Message) + "' does not exist");
                _dte.StatusBar.Text = "Bundle was not created";
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show("Error generating the bundle. See output window for details");
            }
        }

        private async Task MakeBundleAsync(string extension)
        {
            string bundleFile;

            if (!GetFileName(out bundleFile, extension))
                return;

            try
            {
                BundleDocument doc = new BundleDocument(bundleFile, _files.ToArray());

                await doc.WriteBundleRecipe();
                await GenerateAsync(doc, extension);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show("Error generating the bundle. See output window for details");
                _dte.StatusBar.Text = "Bundle was not created";
            }
        }

        private async Task GenerateAsync(BundleDocument bundle, string extension, bool hasUpdated = false)
        {
            _dte.StatusBar.Text = "Generating bundle...";

            string bundleFile = Path.Combine(Path.GetDirectoryName(bundle.FileName), Path.GetFileNameWithoutExtension(bundle.FileName));
            bool hasChanged = await BundleGenerator.MakeBundle(bundle, bundleFile, UpdateBundleAsync);

            if (!hasUpdated)
            {
                ProjectHelpers.AddFileToActiveProject(bundle.FileName);
                ProjectHelpers.AddFileToProject(bundle.FileName, bundleFile);
                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(bundle.FileName);
            }

            if (bundle.Minified)
                await BundleGenerator.MakeMinFile(bundleFile, extension, hasChanged);

            _dte.StatusBar.Text = "Bundle generated";
        }
    }
}
