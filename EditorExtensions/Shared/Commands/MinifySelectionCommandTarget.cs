using System;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class MinifySelection : CommandTargetBase<MinifyCommandId>
    {
        private Tuple<SnapshotSpan, SnapshotSpan> _spansTuple;

        public MinifySelection(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, MinifyCommandId.MinifySelection)
        { }

        protected override bool Execute(MinifyCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_spansTuple == null)
                return false;

            var source = _spansTuple.Item2.GetText();
            string result = Mef.GetImport<IFileMinifier>(_spansTuple.Item2.Snapshot.TextBuffer.ContentType)
                               .MinifyString(source);

            if (result == null)
                return false; // IFileMinifier already displayed an error

            if (result == source)
            {
                WebEssentialsPackage.DTE.StatusBar.Text = "The selection is already minified";
                return false;
            }

            using (WebEssentialsPackage.UndoContext("Minify"))
                TextView.TextBuffer.Replace(_spansTuple.Item1, result);

            return true;
        }

        protected override bool IsEnabled()
        {
            // Don't minify Markdown
            _spansTuple = TextView.GetSelectedSpan(c => !c.IsOfType("Markdown")
                                                     && Mef.GetImport<IFileMinifier>(c) != null);
            return _spansTuple != null;
        }
    }
}