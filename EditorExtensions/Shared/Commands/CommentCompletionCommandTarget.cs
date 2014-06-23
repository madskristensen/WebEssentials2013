using System;
using System.Linq;
using System.Runtime.InteropServices;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    class CommentCompletionCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private IClassifier _classifier;
        public CommentCompletionCommandTarget(IVsTextView adapter, IWpfTextView textView, IClassifierAggregatorService classifier)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.TYPECHAR)
        {
            _classifier = classifier.GetClassifier(textView.TextBuffer);
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!WESettings.Instance.JavaScript.BlockCommentCompletion)
                return false;

            char typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

            if (typedChar != '*')
                return false;

            return CompleteComment();
        }

        private bool CompleteComment()
        {
            int position = TextView.Caret.Position.BufferPosition.Position;

            if (position < 1)
                return false;

            SnapshotSpan span = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, position - 1, 1);
            bool isComment = _classifier.GetClassificationSpans(span).Any(c => c.ClassificationType.IsOfType("comment"));

            if (isComment)
                return false;

            char prevChar = TextView.TextBuffer.CurrentSnapshot.ToCharArray(position - 1, 1)[0];

            // Abort if the previous characters isn't a forward-slash
            if (prevChar != '/' || isComment)
                return false;

            // Insert the typed character
            TextView.TextBuffer.Insert(position, "*");

            using (WebEssentialsPackage.UndoContext("Comment completion"))
            {
                // Use separate undo context for this, so it can be undone separately.
                TextView.TextBuffer.Insert(position + 1, "*/");
            }

            // Move the caret to the correct point
            SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, position + 1);
            TextView.Caret.MoveTo(point);

            return true;
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}