using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    internal class UnusedCssMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public UnusedCssMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(GuidList.guidUnusedCssCmdSet, (int)PkgCmdIDList.cmdUnusedCssSnapshotCommandId);
            OleMenuCommand menuCommand = new OleMenuCommand(SnapshotAll, commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        private void SnapshotAll(object sender, EventArgs e)
        {
            UnusedCssExtension.All(x => x.SnapshotPage());
        }

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            menuCommand.Enabled = UnusedCssExtension.IsAnyConnectionAlive;
        }
    }
}
