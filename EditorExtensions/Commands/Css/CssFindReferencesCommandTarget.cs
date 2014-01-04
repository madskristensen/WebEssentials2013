using System;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class CssFindReferences : CommandTargetBase
    {

        public CssFindReferences(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(VSConstants.VSStd97CmdID).GUID, (uint)VSConstants.VSStd97CmdID.FindReferences)
        {
        }

        protected override bool Execute(CommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("css");
            if (point == null) return false;

            CssEditorDocument doc = CssEditorDocument.FromTextBuffer(point.Value.Snapshot.TextBuffer);

            int position = TextView.Caret.Position.BufferPosition.Position;
            ParseItem item = doc.Tree.StyleSheet.ItemBeforePosition(position);

            if (item != null && item.Parent != null)
            {
                string term = SearchText(item);
                FileHelpers.SearchFiles(term, "*.css;*.less;*.scss;*.sass");
            }

            return true;
        }

        private static string SearchText(ParseItem item)
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

        protected override bool IsEnabled()
        {
            return TextView.GetSelection("css").HasValue;
        }
    }
}