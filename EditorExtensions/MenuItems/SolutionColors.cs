using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class SolutionColorsMenu
    {
        private OleMenuCommandService _mcs;

        public SolutionColorsMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandSol = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.CreateSolutionColorPalete);
            OleMenuCommand menuCommandSol = new OleMenuCommand((s, e) => ApplySolutionSettings(), commandSol);
            menuCommandSol.BeforeQueryStatus += SolutionBeforeQueryStatus;
            _mcs.AddCommand(menuCommandSol);
        }

        private void SolutionBeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            bool settingsExist = XmlColorPaletteProvider.SolutionColorsExist;

            menuCommand.Enabled = !settingsExist;
        }

        private static void ApplySolutionSettings()
        {
            XmlColorPaletteProvider.CreateSolutionColors();
        }
    }
}