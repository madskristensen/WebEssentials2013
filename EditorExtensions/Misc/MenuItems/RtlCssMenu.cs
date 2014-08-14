using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Compilers;
using MadsKristensen.EditorExtensions.RtlCss;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class RtlCssMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public RtlCssMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.RunRtlCss);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => RunRtlCss(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);

            CommandID edit = new CommandID(CommandGuids.guidDiffCmdSet, (int)CommandId.EditGlobalRtlCss);
            OleMenuCommand editCommand = new OleMenuCommand((s, e) => EditGlobalRtlCssFile(), edit);
            _mcs.AddCommand(editCommand);
        }

        private void EditGlobalRtlCssFile()
        {
            string fileName = NodeExecutorBase.GetOrCreateGlobalSettings(RtlCssCompiler.ConfigFileName);

            _dte.ItemOperations.OpenFile(fileName);
        }

        private List<string> files;

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            files = ProjectHelpers.GetSelectedFilePaths()
                                  .Where(f => !MinifiedOrIncorrectExtOrRtlOrNotExists(f)).ToList();

            menuCommand.Enabled = files.Count > 0;
        }

        private static bool MinifiedOrIncorrectExtOrRtlOrNotExists(string file)
        {
            return !file.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".rtl.css", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".rtl.min.css", StringComparison.OrdinalIgnoreCase) ||
                   !File.Exists(file);
        }

        private static string GetTargetPath(string file)
        {
            if (file.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase))
                return file.Substring(0, ".min.css".Length) + ".rtl.min.css";

            return Path.ChangeExtension(file, ".rtl.css");
        }

        private void RunRtlCss()
        {
            foreach (string file in files)
            {
                new NodeCompilerRunner(Mef.GetContentType("CSS"))
                   .CompileAsync(file, GetTargetPath(file))
                   .DoNotWait("generating RTL variant of " + file);
            }
        }
    }
}
