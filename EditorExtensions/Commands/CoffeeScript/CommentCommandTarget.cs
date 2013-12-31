using System;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class CommentCommandTarget : CommandTargetBase
    {
        public CommentCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(VSConstants.VSStd2KCmdID).GUID, 136, 137)
        { }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            StringBuilder sb = new StringBuilder();
            SnapshotSpan span = GetSpan();            
            string[] lines = span.GetText().Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            switch (commandId)
            {
                case (uint)VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                    Comment(sb, lines);
                    break;

                case (uint)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
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
                sb.AppendLine("#" + line);
            }
        }

        private void Uncomment(StringBuilder sb, string[] lines)
        {
            foreach (string line in lines)
            {
                sb.AppendLine(line.TrimStart('#'));
            }
        }

        private void UpdateTextBuffer(SnapshotSpan span, string text)
        {
            using (EditorExtensionsPackage.UndoContext("Comment/Uncomment"))
            {
                TextView.TextBuffer.Replace(span.Span, text.Trim());
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