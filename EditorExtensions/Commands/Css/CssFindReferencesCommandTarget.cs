using System;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

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
                string term = SearchText(item);
                FileHelpers.SearchFiles(term, "*.css;*.less;*.scss;*.sass");
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