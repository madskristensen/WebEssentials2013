using System;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class CommentCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private string _symbol;

        public CommentCommandTarget(IVsTextView adapter, IWpfTextView textView, string commentSymbol)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.COMMENT_BLOCK, VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK)
        {
            _symbol = commentSymbol;
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            StringBuilder sb = new StringBuilder();
            SnapshotSpan span = GetSpan();
            string[] lines = span.GetText().Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            switch (commandId)
            {
                case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                    Comment(sb, lines);
                    break;

                case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
                    Uncomment(sb, lines);
                    break;
            }

            UpdateTextBuffer(span, sb.ToString());

            return true;
        }

        private void Comment(StringBuilder sb, string[] lines)
        {
            foreach (string line in lines)
            {
                sb.AppendLine(_symbol + line);
            }
        }

        private void Uncomment(StringBuilder sb, string[] lines)
        {
            foreach (string line in lines)
            {
                if (line.StartsWith(_symbol, StringComparison.Ordinal))
                {
                    sb.AppendLine(line.Substring(_symbol.Length));
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
        }

        private void UpdateTextBuffer(SnapshotSpan span, string text)
        {
            using (WebEssentialsPackage.UndoContext("Comment/Uncomment"))
            {
                TextView.TextBuffer.Replace(span.Span, text);
            }
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
            return true;
        }
    }
}