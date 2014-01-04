using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    internal class UnusedCssMenu
    {
        private readonly OleMenuCommandService _mcs;

        public UnusedCssMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            var commandId = new CommandID(CommandGuids.guidUnusedCssCmdSet, (int)CommandId.UnusedCssReset);
            var resetCommand = new OleMenuCommand(ResetUsageData, EmptyChangeHandler, ResetUsageDataBeforeQueryStatus, commandId);

            _mcs.AddCommand(resetCommand);

            commandId = new CommandID(CommandGuids.guidUnusedCssCmdSet, (int)CommandId.UnusedCssRecordAll);

            var recordAllCommand = new OleMenuCommand(RecordAll, EmptyChangeHandler, RecordAllBeforeQueryStatus, commandId);

            _mcs.AddCommand(recordAllCommand);

            commandId = new CommandID(CommandGuids.guidUnusedCssCmdSet, (int)CommandId.UnusedCssStopRecordAll);

            var stopRecordAllCommand = new OleMenuCommand(StopRecordAll, EmptyChangeHandler, StopRecordAllBeforeQueryStatus, commandId);

            _mcs.AddCommand(stopRecordAllCommand);
        }

        private static void EmptyChangeHandler(object sender, EventArgs e)
        {
        }

        private static void RecordAll(object sender, EventArgs e)
        {
            UnusedCssExtension.All(x => x.EnsureRecordingMode(true));
        }

        private static void RecordAllBeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = (OleMenuCommand)sender;

            menuCommand.Enabled = UnusedCssExtension.IsAnyConnectionAlive;
            menuCommand.Visible = menuCommand.Enabled && !UnusedCssExtension.Any(x => x.IsRecording);
        }

        private static void ResetUsageData(object sender, EventArgs e)
        {
            DocumentFactory.Clear();
            UsageRegistry.Reset();
            MessageDisplayManager.Refresh();
            UnusedCssExtension.All(x => x.BlipRecording());
        }

        private static void ResetUsageDataBeforeQueryStatus(object sender, EventArgs e)
        {
            var menu = (OleMenuCommand)sender;

            menu.Enabled = UsageRegistry.IsAnyUsageDataCaptured;
        }

        private static void StopRecordAll(object sender, EventArgs e)
        {
            UnusedCssExtension.All(x => x.EnsureRecordingMode(false));
        }

        private static void StopRecordAllBeforeQueryStatus(object sender, EventArgs e)
        {
            var menu = (OleMenuCommand)sender;

            menu.Visible = UnusedCssExtension.Any(x => x.IsRecording);
        }
    }
}
