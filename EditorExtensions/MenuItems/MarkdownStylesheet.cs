using System.ComponentModel.Design;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class MarkdownStylesheetMenu
    {
        private OleMenuCommandService _mcs;

        public MarkdownStylesheetMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdMarkdownStylesheet);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => AddStylesheet(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            menuCommand.Enabled = string.IsNullOrEmpty(MarkdownMargin.GetStylesheet());
        }

        private static void AddStylesheet()
        {
            MarkdownMargin.CreateStylesheet();
        }
    }
}