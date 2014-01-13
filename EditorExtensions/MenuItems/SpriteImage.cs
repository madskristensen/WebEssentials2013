using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.IO;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    internal class SpriteImageMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private IEnumerable<string> _selectedPaths;
        private string _extension = ".png";

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

            _selectedPaths = MinifyFileMenu.GetSelectedFilePaths(_dte)
                                .Where(image => new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" }
                                                           .Contains(Path.GetExtension(image))); ;

            button.Enabled = _selectedPaths.Count() > 1;
        }

        private async Task CreateSprite()
        {
            string dir = Path.GetDirectoryName(_selectedPaths.First());

            if (!Directory.Exists(dir))
                return;

            var rectangles = _selectedPaths.Select(path =>
                                               {
                                                   var image = Image.FromFile(path);
                                                   return new ImageInfo(
                                                        (int)image.Width,
                                                        (int)image.Height,
                                                        path);
                                               }
                                        );

            var spriteFile = Microsoft.VisualBasic.Interaction.InputBox("Specify the name of the sprite", "Web Essentials", "sprite1");

            if (string.IsNullOrEmpty(spriteFile))
                return;

            if (!spriteFile.EndsWith(_extension, StringComparison.OrdinalIgnoreCase))
                spriteFile = spriteFile + _extension;

            spriteFile = Path.Combine(dir, spriteFile);

            if (File.Exists(spriteFile))
            {
                Logger.ShowMessage("The sprite file already exists.");
            }
            else
            {
                SpriteRunner runner = new SpriteRunner(rectangles);
                runner.GenerateSpriteWithMaps(spriteFile);

                ProjectHelpers.AddFileToActiveProject(spriteFile);
                ProjectHelpers.AddFileToProject(spriteFile, spriteFile + ".map");

                await new ImageCompressor().CompressFiles(new[] { spriteFile });
            }
        }
    }
}