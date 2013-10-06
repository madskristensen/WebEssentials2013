using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class ReferenceJsMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private string _referencesJsPath;

        public ReferenceJsMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(GuidList.guidEditorExtensionsCmdSet, (int)PkgCmdIDList.ReferenceJs);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Execute(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            var files = new List<string>(ProjectHelpers.GetSelectedItemPaths());

            if (files.Count == 1 && !Path.HasExtension(files[0]))
            {
                string folder = files[0];
                DirectoryInfo dir = new DirectoryInfo(folder);
                bool isScripts = dir.Name.Equals("scripts", StringComparison.OrdinalIgnoreCase);
                
                if (!isScripts)
                    return;

                _referencesJsPath = Path.Combine(folder, "_references.js");
                bool exist = File.Exists(_referencesJsPath);
                menuCommand.Visible = !exist;
            }
        }

        private void Execute()
        {
            try
            {
                File.WriteAllText(_referencesJsPath, "/// <autosync enabled=\"true\" />", Encoding.UTF8);
                ProjectHelpers.AddFileToActiveProject(_referencesJsPath);
                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(_referencesJsPath);
            }
            catch (IOException)
            {
                System.Windows.Forms.MessageBox.Show("Can't write to the folder");
            }
        }
    }
}