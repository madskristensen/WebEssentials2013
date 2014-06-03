using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    internal class UnminifyMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;
        private ITextBuffer _buffer;

        public UnminifyMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
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
            _buffer  = ProjectHelpers.GetCurentTextBuffer();

            menuCommand.Enabled = _buffer != null && _buffer.ContentType.IsOfType("javascript");
        }

        private void Minify()
        {
            string text = _buffer.CurrentSnapshot.GetText();
            text = Regex.Replace(text, @"(},|};|}\s+|\),|\);|}(?=\w)|(?=if\())", "$1" + Environment.NewLine);

            using (EditorExtensionsPackage.UndoContext("Un-Minify"))
            {
                Span span = new Span(0, _buffer.CurrentSnapshot.Length);
                _buffer.Replace(span, text);
                EditorExtensionsPackage.ExecuteCommand("Edit.FormatDocument");
            }
        }
    }
}