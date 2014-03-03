﻿using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Shell;

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
            CommandID commandSol = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.CreateSolutionSettings);
            OleMenuCommand menuCommandSol = new OleMenuCommand((s, e) => ApplySolutionSettings(), commandSol);
            menuCommandSol.BeforeQueryStatus += SolutionBeforeQueryStatus;
            _mcs.AddCommand(menuCommandSol);

            ProjectItemsEvents projectEvents = ((Events2)_dte.Events).ProjectItemsEvents;
            projectEvents.ItemRemoved += ItemRemoved;
            projectEvents.ItemRenamed += ItemRenamed;

            SolutionEvents solutionEvents = ((Events2)_dte.Events).SolutionEvents;
            solutionEvents.ProjectRemoved += ProjectRemoved;
        }

        private void SolutionBeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            bool settingsExist = SettingsStore.SolutionSettingsExist;

            menuCommand.Enabled = !settingsExist;
        }

        private static void ApplySolutionSettings()
        {
            SettingsStore.CreateSolutionSettings();
        }

        private static void ItemRenamed(ProjectItem ProjectItem, string OldName)
        {
            if (OldName.EndsWith(SettingsStore.FileName, StringComparison.OrdinalIgnoreCase) || ProjectItem.Name == SettingsStore.FileName)
                SettingsStore.Load();
        }

        private void ItemRemoved(ProjectItem ProjectItem)
        {
            if (ProjectItem.Name == SettingsStore.FileName &&
                ProjectItem.ContainingProject != null &&
                ProjectItem.ContainingProject.Name == ProjectHelpers.SolutionItemsFolder)
            {
                DeleteSolutionSettings();
            }
        }

        private void ProjectRemoved(Project project)
        {
            if (project.Name == ProjectHelpers.SolutionItemsFolder)
            {
                DeleteSolutionSettings();
            }
        }

        private static void DeleteSolutionSettings()
        {
            string file = SettingsStore.GetSolutionFilePath();

            if (File.Exists(file))
            {
                string text = "The Web Essentials setting file still exist in the solution folder.\r\n\r\nDo you want to delete it?";
                DialogResult result = MessageBox.Show(text, "Web Essentials", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    File.Delete(file);
                    SettingsStore.Load();
                }
                else
                {
                    SettingsStore.UpdateStatusBar("are still applied. The settings file still exists in the solution folder.");
                }
            }
        }
    }
}