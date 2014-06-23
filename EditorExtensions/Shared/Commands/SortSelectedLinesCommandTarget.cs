using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class SortSelectedLines : CommandTargetBase<LinesCommandId>
    {
        public SortSelectedLines(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, LinesCommandId.SortAsc, LinesCommandId.SortDesc)
        {
        }

        protected override bool Execute(LinesCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var span = GetSpan();
            var lines = span.GetText().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                return false;

            string result = SortLines(commandId, lines);

            using (WebEssentialsPackage.UndoContext(("Sort Selected Lines")))
                TextView.TextBuffer.Replace(span.Span, result);

            return true;
        }

        private static string SortLines(LinesCommandId commandId, IEnumerable<string> lines)
        {
            if (commandId == LinesCommandId.SortAsc)
                lines = lines.OrderBy(t => t);
            else
                lines = lines.OrderByDescending(t => t);

            return string.Join(Environment.NewLine, lines);
        }

        private SnapshotSpan GetSpan()
        {
            var sel = TextView.Selection.StreamSelectionSpan;
            var start = new SnapshotPoint(TextView.TextSnapshot, sel.Start.Position).GetContainingLine().Start;
            var end = new SnapshotPoint(TextView.TextSnapshot, sel.End.Position).GetContainingLine().End;

            return new SnapshotSpan(start, end);
        }

        protected override bool IsEnabled()
        {
            return !TextView.Selection.IsEmpty;
        }
    }
}