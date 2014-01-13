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
            CommandID cmd = new CommandID(CommandGuids.guidSpriteImageCmdSet, (int)CommandId.SpriteImage);
            OleMenuCommand menuCmd = new OleMenuCommand(async (s, e) => await CreateSprite(), cmd);
            menuCmd.BeforeQueryStatus += BeforeQueryStatus;
            _mcs.AddCommand(menuCmd);
        }

        void BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand button = sender as OleMenuCommand;

            _selectedPaths = MinifyFileMenu.GetSelectedFilePaths(_dte)
                                .Where(image => new[] { ".jpg", ".jpeg", ".gif", ".bmp", ".ico" }
                                                           .Contains(Path.GetExtension(image))); ;

            int items = _selectedPaths.Count();

            button.Text = "Create Sprite";
            button.Enabled = items > 1;
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

            var destinationSpriteFile = Microsoft.VisualBasic.Interaction.InputBox("Specify the name of the sprite", "Web Essentials", "sprite1");

            if (string.IsNullOrEmpty(destinationSpriteFile))
                return;

            if (!destinationSpriteFile.EndsWith(_extension, StringComparison.OrdinalIgnoreCase))
                destinationSpriteFile = destinationSpriteFile + _extension;

            destinationSpriteFile = Path.Combine(dir, destinationSpriteFile);

            if (File.Exists(destinationSpriteFile))
            {
                Logger.ShowMessage("The sprite file already exists.");
            }
            else
            {
                new SpriteRunner(rectangles).GenerateSpriteWithMaps(destinationSpriteFile);
                await new ImageCompressor().CompressFiles(new[] { destinationSpriteFile });
            }
        }
    }
}