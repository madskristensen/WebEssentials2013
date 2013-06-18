using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;

namespace MadsKristensen.EditorExtensions
{
    internal class SolutionColorsMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public SolutionColorsMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandSol = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdSolutionColors);
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

        private void ApplySolutionSettings()
        {
            XmlColorPaletteProvider.CreateSolutionColors();
        }
    }
}