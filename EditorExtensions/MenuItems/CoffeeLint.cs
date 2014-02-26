using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class CoffeeLintMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public CoffeeLintMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.RunCoffeeLint);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => RunCoffeeLint(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);

            CommandID edit = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.EditGlobalCoffeeLint);
            OleMenuCommand editCommand = new OleMenuCommand((s, e) => EditGlobalCoffeeLintFile(), edit);
            _mcs.AddCommand(editCommand);
        }

        private void EditGlobalCoffeeLintFile()
        {
            string fileName = CoffeeLintCompiler.GetOrCreateGlobalSettings(CoffeeLintCompiler.ConfigFileName);
            _dte.ItemOperations.OpenFile(fileName);
        }

        private List<string> files;
        private static IEnumerable<string> _sourceExtensions = new CoffeeLintCompiler().SourceExtensions;

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            files = ProjectHelpers.GetSelectedFilePaths()
                    .Where(f => _sourceExtensions.Contains(Path.GetExtension(f)))
                    .ToList();

            menuCommand.Enabled = files.Count > 0;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void RunCoffeeLint()
        {
            LintReporter.Reset();            // TODO: Why?

            foreach (string file in files)
            {
                var runner = new LintReporter(new CoffeeLintCompiler(), WESettings.Instance.CoffeeScript, file);
                runner.RunLinterAsync().DontWait("linting " + file);
            }
        }
    }
}