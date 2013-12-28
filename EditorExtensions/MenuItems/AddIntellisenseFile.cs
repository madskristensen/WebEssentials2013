using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    internal class AddIntellisenseFileMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private string _file;

        public AddIntellisenseFileMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID JsId = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdJavaScriptIntellisense);
            OleMenuCommand jsCommand = new OleMenuCommand((s, e) => Execute(".js"), JsId);
            jsCommand.BeforeQueryStatus += JavaScript_BeforeQueryStatus;
            _mcs.AddCommand(jsCommand);

            CommandID tsId = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdTypeScriptIntellisense);
            OleMenuCommand tsCommand = new OleMenuCommand((s, e) => Execute(".d.ts"), tsId);
            tsCommand.BeforeQueryStatus += TypeScript_BeforeQueryStatus;
            _mcs.AddCommand(tsCommand);
        }

        void JavaScript_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            var items = ProjectHelpers.GetSelectedItemPaths(_dte);

            if (items.Count() == 1 && (items.ElementAt(0).EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || items.ElementAt(0).EndsWith(".vb", StringComparison.OrdinalIgnoreCase)))
            {
                _file = items.ElementAt(0);
            }

            menuCommand.Enabled = !string.IsNullOrEmpty(_file) && !File.Exists(_file + ".js");
        }

        void TypeScript_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            var items = ProjectHelpers.GetSelectedItemPaths(_dte);

            if (items.Count() == 1 && (items.ElementAt(0).EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || items.ElementAt(0).EndsWith(".vb", StringComparison.OrdinalIgnoreCase)))
            {
                _file = items.ElementAt(0);
            }

            menuCommand.Enabled = !string.IsNullOrEmpty(_file) && !File.Exists(_file + ".d.ts");
        }

        protected async Task<bool> Execute(string extension)
        {
            File.WriteAllText(_file + extension, string.Empty);
            if (await ScriptIntellisense.Process(_file))
                return true;
            File.Delete(_file + extension);
            Logger.ShowMessage("An error occurred while processing " + Path.GetFileName(_file) + ".\nNo script file was generated.  For more details, see the output window.");
            return false;
        }
    }
}