using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class JsCodeStyle
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public JsCodeStyle(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.RunJsCodeStyle);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => RunJsCodeStyle(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);

            CommandID edit = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.EditGlobalJsCodeStyle);
            OleMenuCommand editCommand = new OleMenuCommand((s, e) => EditGlobalJsCodeStyleFile(), edit);
            _mcs.AddCommand(editCommand);
        }

        private void EditGlobalJsCodeStyleFile()
        {
            string fileName = JsCodeStyleCompiler.GetOrCreateGlobalSettings(JsCodeStyleCompiler.ConfigFileName);

            _dte.ItemOperations.OpenFile(fileName);
        }

        private List<string> files;

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            files = ProjectHelpers.GetSelectedFilePaths()
                    .Where(f => !JavaScriptLintReporter.NotJsOrMinifiedOrDocumentOrNotExists(f)).ToList();

            menuCommand.Enabled = files.Count > 0;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void RunJsCodeStyle()
        {
            LintReporter.Reset();            // TODO: Why?

            foreach (string file in files)
            {
                var runner = new LintReporter(new JsCodeStyleCompiler(), WESettings.Instance.JavaScript, file);
                runner.RunLinterAsync().DontWait("linting " + file);
            }
        }
    }
}