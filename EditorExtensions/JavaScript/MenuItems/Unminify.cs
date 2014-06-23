using System;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    internal class UnminifyMenu
    {
        private OleMenuCommandService _mcs;
        private ITextBuffer _buffer;

        public UnminifyMenu(OleMenuCommandService mcs)
        {
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(CommandGuids.guidMinifyCmdSet, (int)MinifyCommandId.UnMinifySelection);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Minify(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        void menuCommand_BeforeQueryStatus(object sender, System.EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            _buffer = ProjectHelpers.GetCurentTextBuffer();

            menuCommand.Enabled = _buffer != null && _buffer.ContentType.IsOfType("javascript");
        }

        private void Minify()
        {
            string text = _buffer.CurrentSnapshot.GetText();
            text = Regex.Replace(text, @"(},|};|}\s+|\),|\);|}(?=\w)|(?=if\())", "$1" + Environment.NewLine);

            using (WebEssentialsPackage.UndoContext("Un-Minify"))
            {
                Span span = new Span(0, _buffer.CurrentSnapshot.Length);
                _buffer.Replace(span, text);
                WebEssentialsPackage.ExecuteCommand("Edit.FormatDocument");
            }
        }
    }
}