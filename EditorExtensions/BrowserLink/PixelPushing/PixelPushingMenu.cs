using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.PixelPushing
{
    internal class PixelPushingMenu
    {
        private readonly OleMenuCommandService _mcs;

        public PixelPushingMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            var commandId = new CommandID(CommandGuids.guidPixelPushingCmdSet, (int)CommandId.PixelPushingToggle);
            var toggleCommand = new OleMenuCommand(TogglePixelPushingMode, EmptyChangeHandler, TogglePixelPushingModeBeforeQueryStatus, commandId)
            {
                Checked = true
            };

            _mcs.AddCommand(toggleCommand);
        }

        private static void EmptyChangeHandler(object sender, EventArgs e)
        {
        }

        private static void TogglePixelPushingModeBeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = (OleMenuCommand)sender;

            menuCommand.Checked = PixelPushingMode.IsPixelPushingModeEnabled;
        }

        private static void TogglePixelPushingMode(object sender, EventArgs e)
        {
            PixelPushingMode.IsPixelPushingModeEnabled = !PixelPushingMode.IsPixelPushingModeEnabled;

            PixelPushingMode.All(x => x.SetMode());
        }
    }
}
