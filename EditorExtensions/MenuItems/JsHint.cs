using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;

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
            CommandID commandId = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdJsHint);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => RunJsHint(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        private List<string> files;

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            var raw = MinifyFileMenu.GetSelectedFilePaths(_dte);
            files = raw.Where(f => !JsHintRunner.ShouldIgnore(f)).ToList();

            menuCommand.Enabled = files.Count > 0;
        }

        private void RunJsHint()
        {
            JsHintRunner.Reset();

            foreach (string file in files)
            {
                JsHintRunner runner = new JsHintRunner(file);
                runner.RunCompiler();
            }
        }
    }
}