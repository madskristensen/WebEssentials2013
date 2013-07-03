using CssSorter;
using EnvDTE;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class CssFindReferences : CommandTargetBase
    {
        private DTE2 _dte;
        private CssTree _tree;

        public CssFindReferences(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(VSConstants.VSStd97CmdID).GUID, (uint)VSConstants.VSStd97CmdID.FindReferences)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!EnsureInitialized())
                return false;

            int position = TextView.Caret.Position.BufferPosition.Position;
            ParseItem item = _tree.StyleSheet.ItemBeforePosition(position);

            if (item != null && item.Parent != null)
            {
                Find2 find = (Find2)EditorExtensionsPackage.DTE.Find;
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
                find.FilesOfType = "*.css;*.less;*.scss;*.sass";
                find.Target = EnvDTE.vsFindTarget.vsFindTargetSolution;
                find.FindWhat = SearchText(item);
                find.Execute();

                find.FilesOfType = types;
                find.MatchCase = matchCase;
                find.MatchWholeWord = matchWord;
            }

            return true;
        }

        private string SearchText(ParseItem item)
        {
            if (item.Parent is Declaration)
            {
                return item.Text;
            }
            else if (item.Parent is AtDirective)
            {
                return "@" + item.Text;
            }

            return item.Parent.Text;
        }

        public bool EnsureInitialized()
        {
            if (_tree == null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(TextView.TextBuffer);
                    _tree = document.Tree;
                }
                catch (ArgumentNullException)
                { }
            }

            return _tree != null;
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}