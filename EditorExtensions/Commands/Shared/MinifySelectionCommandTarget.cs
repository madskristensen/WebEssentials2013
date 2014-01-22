using System;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class MinifySelection : CommandTargetBase
    {

        public MinifySelection(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, CommandGuids.guidMinifyCmdSet, CommandId.MinifySelection)
        {
        }

        SnapshotSpan? span;
        protected override bool Execute(CommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (span == null)
                return false;
            var source = span.Value.GetText();
            string result = Mef.GetImport<IFileMinifier>(span.Value.Snapshot.TextBuffer.ContentType)
                               .MinifyString(source);

            if (result == null)
                return false; // IFileMinifier already displayed an error
            if (result == source) { 
                EditorExtensionsPackage.DTE.StatusBar.Text = "The selection is already minified";
                return false;
            }
            using (EditorExtensionsPackage.UndoContext("Minify"))
                TextView.TextBuffer.Replace(span.Value.Span, result);

            return true;
        }

        protected override bool IsEnabled()
        {
            // Don't minify Markdown
            span = TextView.GetSelectedSpan(c => !c.IsOfType("Markdown")
                                              && Mef.GetImport<IFileMinifier>(c) != null);
            return span.HasValue;
        }
    }
}