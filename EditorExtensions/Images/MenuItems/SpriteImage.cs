using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions.Images
{
    internal class SpriteImageMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private IEnumerable<string> _files;
        private static string[] _supported = new[] { ".png", ".jpg", ".jpeg", ".gif" };

        public SpriteImageMenu()
        {
            // Used by the IWpfTextViewCreationListener
            _dte = EditorExtensionsPackage.DTE;
        }

        public SpriteImageMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID cmd = new CommandID(CommandGuids.guidImageCmdSet, (int)CommandId.SpriteImage);
            OleMenuCommand menuCmd = new OleMenuCommand(async (s, e) => await MakeSpriteAsync(), cmd);
            menuCmd.BeforeQueryStatus += BeforeQueryStatus;
            _mcs.AddCommand(menuCmd);
        }

        private void BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand button = sender as OleMenuCommand;

            _files = ProjectHelpers.GetSelectedFilePaths()
                                   .Where(file => _supported.Any(x => x.Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase)));

            button.Enabled = _files.Count() > 1;
        }

        private async Task MakeSpriteAsync()
        {
            string spriteFile;

            if (!GetFileName(out spriteFile))
                return;

            try
            {
                SpriteDocument doc = new SpriteDocument(spriteFile, _files.ToArray());

                await doc.WriteSpriteRecipe();
                await GenerateAsync(doc);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show("Error generating the sprite. See output window for details");
                _dte.StatusBar.Text = "Sprite was not created";
            }
        }

        public static async Task UpdateAllSpritesAsync(bool isBuild = false)
        {
            foreach (Project project in ProjectHelpers.GetAllProjects())
            {
                if (project.ProjectItems.Count == 0)
                    continue;

                string folder = ProjectHelpers.GetRootFolder(project);

                foreach (string file in Directory.EnumerateFiles(folder, "*.sprite", SearchOption.AllDirectories))
                {
                    if (ProjectHelpers.GetProjectItem(file) == null)
                        continue;

                    await new SpriteImageMenu().UpdateSpriteAsync(file, isBuild);
                }
            }
        }

        private async Task UpdateSpriteAsync(string spriteFileName, bool isBuild = false)
        {
            if (!spriteFileName.EndsWith(".sprite", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                SpriteDocument doc = SpriteDocument.FromFile(spriteFileName);

                if (!isBuild || doc.RunOnBuild)
                    await GenerateAsync(doc, true);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Log(ex.Message);
                MessageBox.Show("The file '" + Path.GetFileName(ex.Message) + "' does not exist");
                _dte.StatusBar.Text = "Sprite was not created";
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show("Error generating the sprite. See output window for details");
            }
        }

        private async Task GenerateAsync(SpriteDocument sprite, bool hasUpdated = false)
        {
            _dte.StatusBar.Text = "Generating sprite...";

            //Default file name is the sprite name with specified file extension.
            string imageFile = Path.ChangeExtension(sprite.FileName, sprite.FileExtension);

            IEnumerable<SpriteFragment> fragments = await SpriteGenerator.MakeImage(sprite, imageFile, UpdateSpriteAsync);

            if (!hasUpdated)
            {
                ProjectHelpers.AddFileToActiveProject(sprite.FileName);
                ProjectHelpers.AddFileToProject(sprite.FileName, imageFile);
                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(sprite.FileName);
            }

            await Export(fragments, imageFile, sprite);

            if (sprite.Optimize)
                await new ImageCompressor().CompressFilesAsync(imageFile);

            _dte.StatusBar.Text = "Sprite generated";
        }

        private async static Task Export(IEnumerable<SpriteFragment> fragments, string imageFile, SpriteDocument sprite)
        {
            foreach (ExportFormat format in (ExportFormat[])Enum.GetValues(typeof(ExportFormat)))
            {
                string exportFile = await SpriteExporter.Export(fragments, sprite, imageFile, format);
                ProjectHelpers.AddFileToProject(imageFile, exportFile);
            }
        }

        private bool GetFileName(out string fileName)
        {
            fileName = "MySprite";

            string directory = Path.GetDirectoryName(_files.First());

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = fileName;
                dialog.DefaultExt = ".sprite";
                dialog.Filter = "Sprite File|*.sprite";
                dialog.InitialDirectory = directory;

                if (dialog.ShowDialog() != DialogResult.OK)
                    return false;

                fileName = dialog.FileName;
            }

            //Check for colliding file names
            string collidedFile = FileHelpers.GetFileCollisions(Path.ChangeExtension(fileName, Path.GetExtension(_files.First())), ".css", ".scss", ".less", ".map");
            if (collidedFile == null)
            {
                return true;
            }

            if (MessageBox.Show("The following existing file conflicts with a file that would be generated:\r\n'" + collidedFile + "'.\r\n\r\nWould you like to retry with a different name or cancel?", "File name issue", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation) == DialogResult.Retry)
            {
                return GetFileName(out fileName);
            }

            _dte.StatusBar.Text = "Sprite generation canceled.";
            return false;
        }
    }
}
