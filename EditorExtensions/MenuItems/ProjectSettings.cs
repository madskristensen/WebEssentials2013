using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using System.IO;
using System.Windows.Forms;

namespace MadsKristensen.EditorExtensions
{
    internal class ProjectSettingsMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public ProjectSettingsMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandSol = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdSolutionSettings);
            OleMenuCommand menuCommandSol = new OleMenuCommand((s, e) => ApplySolutionSettings(), commandSol);
            menuCommandSol.BeforeQueryStatus += SolutionBeforeQueryStatus;
            _mcs.AddCommand(menuCommandSol);

            ProjectItemsEvents projectEvents = ((Events2)_dte.Events).ProjectItemsEvents;
            projectEvents.ItemRemoved += ItemRemoved;
            projectEvents.ItemRenamed += ItemRenamed;

            SolutionEvents solutionEvents = ((Events2)_dte.Events).SolutionEvents;
            solutionEvents.ProjectRemoved += ProjectRemoved;
        }

        private void SolutionBeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            bool settingsExist = Settings.SolutionSettingsExist;

            menuCommand.Enabled = !settingsExist;
        }

        private void ApplySolutionSettings()
        {
            Settings.CreateSolutionSettings();
        }

        private void ItemRenamed(ProjectItem ProjectItem, string OldName)
        {
            if (OldName.EndsWith(Settings._fileName) || ProjectItem.Name == Settings._fileName)
                Settings.UpdateCache();
        }

        private void ItemRemoved(ProjectItem ProjectItem)
        {
            if (ProjectItem.Name == Settings._fileName &&
                ProjectItem.ContainingProject != null &&
                ProjectItem.ContainingProject.Name == Settings._solutionFolder)
            {
                DeleteSolutionSettings();
            }
        }

        private void ProjectRemoved(Project project)
        {
            if (project.Name == Settings._solutionFolder)
            {
                DeleteSolutionSettings();
            }
        }

        private static void DeleteSolutionSettings()
        {
            string file = Settings.GetSolutionFilePath();

            if (File.Exists(file))
            {
                string text = "The Web Essentials setting file still exist in the solution folder.\r\n\r\nDo you want to delete it?";
                DialogResult result = MessageBox.Show(text, "Web Essentials", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    File.Delete(file);
                    Settings.UpdateCache();
                    Settings.UpdateStatusBar("applied");
                }
                else
                {
                    Settings.UpdateStatusBar("still applies. The settings file still exist in the solution folder.");
                }
            }
        }
    }
}