using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
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
        private IEnumerable<string> _files;
        private static string[] _supported = new[] { ".png", ".jpg", ".jpeg", ".gif" };

        public SpriteImageMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID cmd = new CommandID(CommandGuids.guidImageCmdSet, (int)CommandId.SpriteImage);
            OleMenuCommand menuCmd = new OleMenuCommand(async (s, e) => await CreateSprite(), cmd);
            menuCmd.BeforeQueryStatus += BeforeQueryStatus;
            _mcs.AddCommand(menuCmd);
        }

        void BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand button = sender as OleMenuCommand;

            _files = MinifyFileMenu.GetSelectedFilePaths(_dte)
                                   .Where(file => _supported.Contains(Path.GetExtension(file)));

            button.Enabled = _files.Count() > 1;
        }

        private async Task CreateSprite()
        {
            string spriteFile;

            if (!GetFileName(out spriteFile))
                return;

            var rectangles = _files.Select(path =>
            {
                var image = Image.FromFile(path);
                return new ImageInfo(image.Width, image.Height, path);
            });

            SpriteGenerator runner = new SpriteGenerator(rectangles);
            runner.GenerateSpriteWithMaps(spriteFile);

            ProjectHelpers.AddFileToActiveProject(spriteFile);
            ProjectHelpers.AddFileToProject(spriteFile, spriteFile + SpriteGenerator.MapExtension);

            await new ImageCompressor().CompressFiles(spriteFile);
        }

        private bool GetFileName(out string fileName)
        {
            fileName = "sprite";
            string firstFile = _files.First();
            string directory = Path.GetDirectoryName(firstFile);
            string extension = Path.GetExtension(firstFile);

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = fileName;
                dialog.DefaultExt = extension;
                dialog.Filter = "Images|*.png;*.gif;*.jpg";
                dialog.InitialDirectory = directory;

                if (dialog.ShowDialog() != DialogResult.OK)
                    return false;

                fileName = dialog.FileName;
            }

            return true;
        }
    }
}