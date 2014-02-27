using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    internal class SpriteImageMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private IEnumerable<string> _files, _sprites;
        private static string[] _supported = new[] { ".png", ".jpg", ".jpeg", ".gif" };

        public SpriteImageMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID cmd = new CommandID(CommandGuids.guidImageCmdSet, (int)CommandId.SpriteImage);
            OleMenuCommand menuCmd = new OleMenuCommand(async (s, e) => await CreateSpriteAsync(), cmd);
            menuCmd.BeforeQueryStatus += BeforeQueryStatus;
            _mcs.AddCommand(menuCmd);

            CommandID update = new CommandID(CommandGuids.guidImageCmdSet, (int)CommandId.UpdateSprite);
            OleMenuCommand menuUpdate = new OleMenuCommand(async (s, e) => await UpdateSpriteAsync(), update);
            menuUpdate.BeforeQueryStatus += IsSpriteFile;
            _mcs.AddCommand(menuUpdate);
        }

        void IsSpriteFile(object sender, System.EventArgs e)
        {
            OleMenuCommand button = sender as OleMenuCommand;
            _sprites = ProjectHelpers.GetSelectedFilePaths()
                                   .Where(file => Path.GetExtension(file) == ".sprite");

            button.Enabled = _sprites.Count() > 0;
        }

        void BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand button = sender as OleMenuCommand;

            _files = ProjectHelpers.GetSelectedFilePaths()
                                   .Where(file => _supported.Contains(Path.GetExtension(file)));

            button.Enabled = _files.Count() > 1;
        }

        private async Task CreateSpriteAsync()
        {
            string spriteFile;

            if (!GetFileName(out spriteFile))
                return;

            try
            {
                SpriteDocument doc = new SpriteDocument(spriteFile, _files.ToArray());
                doc.Save();
                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(spriteFile);

                await GenerateAsync(doc);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show("Error generating the sprite. See output window for details");
                _dte.StatusBar.Text = "Sprite was not created";
            }
        }

        private async Task UpdateSpriteAsync()
        {
            try
            {
                await Task.WhenAll(_sprites.Select(async file =>
                {
                    SpriteDocument doc = SpriteDocument.FromFile(file);
                    await GenerateAsync(doc);
                }));
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

        private async Task GenerateAsync(SpriteDocument sprite)
        {
            _dte.StatusBar.Text = "Generating sprite...";

            string imageFile;
            var fragments = SpriteGenerator.CreateImage(sprite, out imageFile);

            ProjectHelpers.AddFileToActiveProject(sprite.FileName);
            ProjectHelpers.AddFileToProject(sprite.FileName, imageFile);

            Export(fragments, imageFile);

            if (sprite.Optimize)
                await new ImageCompressor().CompressFilesAsync(imageFile);

            _dte.StatusBar.Text = "Sprite generated";
        }

        private static void Export(IEnumerable<SpriteFragment> fragments, string imageFile)
        {
            foreach (ExportFormat format in (ExportFormat[])Enum.GetValues(typeof(ExportFormat)))
            {
                string exportFile = SpriteExporter.Export(fragments, imageFile, format);
                ProjectHelpers.AddFileToProject(imageFile, exportFile);
            }
        }

        private bool GetFileName(out string fileName)
        {
            fileName = "MySprite";
            string firstFile = _files.First();
            string directory = Path.GetDirectoryName(firstFile);

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

            return true;
        }
    }
}