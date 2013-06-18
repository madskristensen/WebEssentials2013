using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    internal class MarkdownStylesheetMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public MarkdownStylesheetMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdMarkdownStylesheet);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => AddStylesheet(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }
        
        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            menuCommand.Enabled = string.IsNullOrEmpty(MarkdownMargin.GetStylesheet());
        }

        private void AddStylesheet()
        {
            MarkdownMargin.CreateStylesheet();
        }
    }
}