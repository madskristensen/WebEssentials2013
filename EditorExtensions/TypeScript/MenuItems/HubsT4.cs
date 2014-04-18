using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.TypeScript
{
    internal class HubsT4Menu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        private readonly List<string> _dependencies;

        private IEnumerable<string> selectedFiles;
        private const string scriptTypingsFolder = @"scripts\typings";
        private const string hubsT4FileName = @"Hubs.tt";

        public HubsT4Menu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
            _dependencies = new List<string>();
            _dependencies.Add(@"\typings\jquery\jquery.d.ts");
            _dependencies.Add(@"\typings\signalr\signalr.d.ts");
        }

        public void SetupCommands()
        {
            CommandID addCommandId = new CommandID(CommandGuids.guidTypeScriptTypingsCmdSet, (int)CommandId.AddHubT4);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Execute(), addCommandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        private void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            selectedFiles = ProjectHelpers.GetSelectedFilePaths().Where(IsDependency);
            menuCommand.Visible = selectedFiles.Any();
        }

        private bool IsDependency(string fullPath)
        {
            foreach (var item in _dependencies)
            {
                if (fullPath.EndsWith(item, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void Execute()
        {
            try
            {
                string parentPath = selectedFiles.FirstOrDefault(o => o.ToLower(CultureInfo.CurrentCulture).Contains(scriptTypingsFolder));
                parentPath = parentPath.Remove(parentPath.ToLower(CultureInfo.CurrentCulture).IndexOf(scriptTypingsFolder, StringComparison.OrdinalIgnoreCase) + scriptTypingsFolder.Length);
                string fullPath = Path.Combine(parentPath, hubsT4FileName);

                if (File.Exists(fullPath))
                {
                    MessageBox.Show("A Hubs.tt file already exists", "Web Essentials", MessageBoxButtons.OK);
                    return;
                }

                string extensionDir = Path.GetDirectoryName(typeof(HubsT4Menu).Assembly.Location);
                string settingsFile = Path.Combine(extensionDir, @"Resources\settings-defaults\" + hubsT4FileName);
                File.Copy(settingsFile, fullPath);

                ProjectHelpers.AddFileToActiveProject(fullPath);

                _dte.ItemOperations.OpenFile(fullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong =(" + Environment.NewLine + Environment.NewLine + ex.ToString(), "Web Essentials", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }


    }
}
