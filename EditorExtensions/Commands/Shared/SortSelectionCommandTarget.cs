using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class SortSelection : CommandTargetBase
    {
        private DTE2 _dte;

        public SortSelection(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidEditorExtensionsCmdSet, PkgCmdIDList.cmdidSortAsc, PkgCmdIDList.cmdidSortDesc)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var span = TextView.Selection.SelectedSpans[0];
            var lines = TextView.TextViewLines.GetTextViewLinesIntersectingSpan(span).ToArray();

            var text = TextView.TextBuffer.CurrentSnapshot.GetText(lines.First().Start, lines.Last().End - lines.First().Start);
            var textLines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (commandId == PkgCmdIDList.cmdidSortAsc)
                Array.Sort(textLines, (a, b) => { return a.Trim().CompareTo(b.Trim()); });
            else
                Array.Sort(textLines, (a, b) => { return b.Trim().CompareTo(a.Trim()); });

            string result = string.Join(Environment.NewLine, textLines);
            var lineSpan = new Span(lines.First().Start, lines.Last().End - lines.First().Start);

            _dte.UndoContext.Open("Alphabetize selected lines");
            TextView.TextBuffer.Replace(lineSpan, result);
            _dte.UndoContext.Close();

            return true;
        }

        protected override bool IsEnabled()
        {
            return !TextView.Selection.IsEmpty;
        }
    }
}