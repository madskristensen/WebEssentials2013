using System;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class RemoveEmptyLines : CommandTargetBase<LinesCommandId>
    {
        public RemoveEmptyLines(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, LinesCommandId.RemoveEmptyLines)
        {
        }

        protected override bool Execute(LinesCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var span = GetSpan();
            var lines = span.GetText().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                return false;

            string result = RemoveLines(lines);

            using (WebEssentialsPackage.UndoContext(("Remove Empty Lines")))
                TextView.TextBuffer.Replace(span.Span, result);

            return true;
        }

        private static string RemoveLines(string[] lines)
        {
            return string.Join(Environment.NewLine, lines.Where(s => !string.IsNullOrWhiteSpace(s)));
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