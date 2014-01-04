using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class ReferenceJsMenu
    {
        private OleMenuCommandService _mcs;
        private string _referencesJsPath;

        public ReferenceJsMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(CommandGuids.guidEditorExtensionsCmdSet, (int)CommandId.ReferenceJs);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Execute(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            menuCommand.Visible = false;

            var projects = ProjectHelpers.GetSelectedProjects().ToList();
            if (projects.Count == 1)
            {
                if (!projects[0].IsWebProject())
                    return;
                _referencesJsPath = Path.Combine(ProjectHelpers.GetRootFolder(projects[0]), @"Scripts\_references.js");
            }
            else
            {
                var files = ProjectHelpers.GetSelectedItemPaths().ToList();

                if (files.Count != 1 || Path.HasExtension(files[0]))
                    return;
                if (!ProjectHelpers.GetSelectedItems().First().ContainingProject.IsWebProject())
                    return;
                string folder = files[0];

                DirectoryInfo dir = new DirectoryInfo(folder);
                bool isScripts = dir.Name.Equals("scripts", StringComparison.OrdinalIgnoreCase);

                if (!isScripts)
                    return;

                _referencesJsPath = Path.Combine(folder, "_references.js");
            }
            if (File.Exists(_referencesJsPath))
                return;
            menuCommand.Visible = true;
        }

        private void Execute()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_referencesJsPath));
                File.WriteAllText(_referencesJsPath, "/// <autosync enabled=\"true\" />", Encoding.UTF8);
                ProjectHelpers.AddFileToActiveProject(_referencesJsPath);
                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(_referencesJsPath);
            }
            catch (IOException)
            {
                Logger.ShowMessage("Can't write to the folder: " + _referencesJsPath);
            }
        }
    }
}