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
            OleMenuCommand menuCommand = new OleMenuCommand(SnapshotAll, EmptyChangeHandler, SnapshotAllBeforeQueryStatus, commandId);
            _mcs.AddCommand(menuCommand);

            commandId = new CommandID(GuidList.guidUnusedCssCmdSet, (int)PkgCmdIDList.cmdUnusedCssResetCommandId);
            OleMenuCommand resetCommand = new OleMenuCommand(ResetUsageData, EmptyChangeHandler, ResetUsageDataBeforeQueryStatus, commandId);
            _mcs.AddCommand(resetCommand);

            commandId = new CommandID(GuidList.guidUnusedCssCmdSet, (int)PkgCmdIDList.cmdUnusedCssRecordAllCommandId);
            OleMenuCommand recordAllCommand = new OleMenuCommand(RecordAll, EmptyChangeHandler, RecordAllBeforeQueryStatus, commandId);
            _mcs.AddCommand(recordAllCommand);

            commandId = new CommandID(GuidList.guidUnusedCssCmdSet, (int)PkgCmdIDList.cmdUnusedCssStopRecordAllCommandId);
            OleMenuCommand stopRecordAllCommand = new OleMenuCommand(StopRecordAll, EmptyChangeHandler, StopRecordAllBeforeQueryStatus, commandId);
            _mcs.AddCommand(stopRecordAllCommand);
        }

        private void RecordAllBeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            menuCommand.Enabled = UnusedCssExtension.IsAnyConnectionAlive;
        }

        private void EmptyChangeHandler(object sender, EventArgs e)
        {
        }

        private void StopRecordAllBeforeQueryStatus(object sender, EventArgs e)
        {
            var menu = (OleMenuCommand)sender;
            menu.Visible = UnusedCssExtension.Any(x => x.IsRecording);
        }

        private void RecordAll(object sender, EventArgs e)
        {
            UnusedCssExtension.All(x => x.EnsureRecordingMode(true));
        }

        private void StopRecordAll(object sender, EventArgs e)
        {
            UnusedCssExtension.All(x => x.EnsureRecordingMode(false));
        }

        private void ResetUsageDataBeforeQueryStatus(object sender, EventArgs e)
        {
            var menu = (OleMenuCommand)sender;
            menu.Enabled = UsageRegistry.IsAnyUsageDataCaptured;
        }

        private void ResetUsageData(object sender, EventArgs e)
        {
            UsageRegistry.Reset();
            MessageDisplayManager.Refresh();
        }

        private void SnapshotAll(object sender, EventArgs e)
        {
            UnusedCssExtension.All(x => x.SnapshotPage());
        }

        private void SnapshotAllBeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            menuCommand.Enabled = UnusedCssExtension.IsAnyConnectionAlive;
        }
    }
}
