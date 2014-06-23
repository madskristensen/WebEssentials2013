using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class DiffMenu
    {
        private OleMenuCommandService _mcs;

        public DiffMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.RunDiff);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => PerformDiff(), commandId);
            menuCommand.BeforeQueryStatus += BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        private List<string> files;

        private void BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            files = new List<string>(ProjectHelpers.GetSelectedItemPaths());

            menuCommand.Enabled = files.Count == 2;
        }

        private void PerformDiff()
        {
            WebEssentialsPackage.ExecuteCommand("Tools.DiffFiles", "\"" + files[0] + "\" \"" + files[1] + "\"");
        }
    }
}