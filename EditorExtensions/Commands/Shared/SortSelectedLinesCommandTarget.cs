using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class SortSelectedLines : CommandTargetBase
    {
        public SortSelectedLines(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, CommandGuids.guidEditorLinesCmdSet, CommandId.SortAsc, CommandId.SortDesc)
        {
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var span = GetSpan();
            var lines = span.GetText().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                return false;

            string result = SortLines((CommandId)commandId, lines);

            using (EditorExtensionsPackage.UndoContext(("Sort Selected Lines")))
                TextView.TextBuffer.Replace(span.Span, result);

            return true;
        }

        private static string SortLines(CommandId commandId, IEnumerable<string> lines)
        {
            if (commandId == CommandId.SortAsc)
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