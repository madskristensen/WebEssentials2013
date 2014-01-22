using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    internal class CompressImageMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private IEnumerable<string> _selectedPaths;

        public CompressImageMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID cmd = new CommandID(CommandGuids.guidImageCmdSet, (int)CommandId.CompressImage);
            OleMenuCommand menuCmd = new OleMenuCommand(async (s, e) => await Compress(), cmd);
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

        private async Task Compress()
        {
            await new ImageCompressor().CompressFiles(_selectedPaths.ToArray());
        }
    }
}