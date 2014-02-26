using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class TsLintMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public TsLintMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.RunTsLint);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => RunTsLint(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);

            CommandID edit = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.EditGlobalTsLint);
            OleMenuCommand editCommand = new OleMenuCommand((s, e) => EditGlobalTsLintFile(), edit);
            _mcs.AddCommand(editCommand);
        }

        private void EditGlobalTsLintFile()
        {
            string fileName = TsLintCompiler.GetOrCreateGlobalSettings(TsLintCompiler.ConfigFileName);
            _dte.ItemOperations.OpenFile(fileName);
        }

        private List<string> files;

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            files = ProjectHelpers.GetSelectedFilePaths()
                    .Where(f => Path.GetExtension(f).Equals(".ts", System.StringComparison.OrdinalIgnoreCase))
                    .ToList();

            menuCommand.Enabled = files.Count > 0;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void RunTsLint()
        {
            LintReporter.Reset();            // TODO: Why?

            foreach (string file in files)
            {
                var runner = new LintReporter(new TsLintCompiler(), WESettings.Instance.TypeScript, file);
                runner.RunLinterAsync().DontWait("linting " + file);
            }
        }
    }
}