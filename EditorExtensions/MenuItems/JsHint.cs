using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class JsHintMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public JsHintMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.RunJsHint);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => RunJsHint(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);

            CommandID edit = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.EditGlobalJsHint);
            OleMenuCommand editCommand = new OleMenuCommand((s, e) => EditGlobalJsHintFile(), edit);
            _mcs.AddCommand(editCommand);
        }

        private void EditGlobalJsHintFile()
        {
            string fileName = JsHintCompiler.GetOrCreateGlobalSettings(JsHintCompiler.ConfigFileName);

            _dte.ItemOperations.OpenFile(fileName);
        }

        private List<string> files;

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            var raw = ProjectHelpers.GetSelectedFilePaths();
            files = raw.Where(f => !JavaScriptLintReporter.NotJsOrMinifiedOrDocumentOrNotExists(f)).ToList();

            menuCommand.Enabled = files.Count > 0;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void RunJsHint()
        {
            LintReporter.Reset();            // TODO: Why?

            foreach (string file in files)
            {
                JavaScriptLintReporter runner = new JavaScriptLintReporter(new JsHintCompiler(), file);
                runner.RunLinterAsync().DontWait("linting " + file);
            }
        }
    }
}