using System;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    internal class JavaScriptFindReferences : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        private ITextStructureNavigator _navigator;

        public JavaScriptFindReferences(IVsTextView adapter, IWpfTextView textView, ITextStructureNavigatorSelectorService navigator)
            : base(adapter, textView, VSConstants.VSStd97CmdID.FindReferences)
        {
            _navigator = navigator.GetTextStructureNavigator(textView.TextBuffer);
        }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            SnapshotPoint? point = TextView.Caret.Position.Point.GetPoint(TextView.TextBuffer, PositionAffinity.Predecessor);

            if (point.HasValue)
            {
                TextExtent wordExtent = _navigator.GetExtentOfWord(point.Value - 1);
                string wordText = TextView.TextSnapshot.GetText(wordExtent.Span);

                Find2 find = (Find2)WebEssentialsPackage.DTE.Find;
                string types = find.FilesOfType;
                bool matchCase = find.MatchCase;
                bool matchWord = find.MatchWholeWord;

                find.WaitForFindToComplete = false;
                find.Action = EnvDTE.vsFindAction.vsFindActionFindAll;
                find.Backwards = false;
                find.MatchInHiddenText = true;
                find.MatchWholeWord = true;
                find.MatchCase = true;
                find.PatternSyntax = EnvDTE.vsFindPatternSyntax.vsFindPatternSyntaxLiteral;
                find.ResultsLocation = EnvDTE.vsFindResultsLocation.vsFindResults1;
                find.SearchSubfolders = true;
                find.FilesOfType = "*.js";
                find.Target = EnvDTE.vsFindTarget.vsFindTargetSolution;
                find.FindWhat = wordText;
                find.Execute();

                find.FilesOfType = types;
                find.MatchCase = matchCase;
                find.MatchWholeWord = matchWord;
            }

            return true;
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}