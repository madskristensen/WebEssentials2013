using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class CompressImageMenu
    {
        private OleMenuCommandService _mcs;
        private IEnumerable<string> _selectedPaths;

        public CompressImageMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID cmd = new CommandID(CommandGuids.guidImageCmdSet, (int)CommandId.CompressImage);
            OleMenuCommand menuCmd = new OleMenuCommand((s, e) => StartCompress(), cmd);
            menuCmd.BeforeQueryStatus += BeforeQueryStatus;
            _mcs.AddCommand(menuCmd);
        }

        void BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand button = sender as OleMenuCommand;
            _selectedPaths = ProjectHelpers.GetSelectedFilePaths()
                                .Where(p => ImageCompressor.IsFileSupported(p));

            int items = _selectedPaths.Count();

            button.Text = items == 1 ? "Optimize image" : "Optimize images";
            button.Enabled = items > 0;
        }

        private void StartCompress()
        {
            new ImageCompressor().CompressFilesAsync(_selectedPaths.ToArray()).DontWait("compressing " + string.Join(", ", _selectedPaths));
        }
    }
}