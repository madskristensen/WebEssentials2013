using System;
using System.Linq;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    class CommentIndentationCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private IClassifier _classifier;
        private ICompletionBroker _broker;

        public CommentIndentationCommandTarget(IVsTextView adapter, IWpfTextView textView, IClassifierAggregatorService classifier, ICompletionBroker broker)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.RETURN)
        {
            _classifier = classifier.GetClassifier(textView.TextBuffer);
            _broker = broker;
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!WESettings.Instance.JavaScript.BlockCommentCompletion || _broker.IsCompletionActive(TextView))
                return false;

            int position = TextView.Caret.Position.BufferPosition.Position;

            if (position < 1 || position > TextView.TextBuffer.CurrentSnapshot.Length - 2)
                return false;

            int checkPosition = position;
            if (TextView.TextBuffer.ContentType.IsOfType("TypeScript"))
            {
                // HACK: TypeScript classifies wrongly, so we have to move the position to 
                // the previous character to test if the caret is in a comment.
                checkPosition -= 1;

                if (TextView.TextBuffer.CurrentSnapshot.GetText(checkPosition, 1) == "/")
                    return false;
            }

            SnapshotSpan span = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, checkPosition, 1);
            bool isComment = _classifier.GetClassificationSpans(span).Any(c => c.ClassificationType.IsOfType("comment"));

            if (!isComment)
                return false;

            ITextSnapshotLine line = TextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(position);
            string text = line.GetText();
            string indentation = new string(text.TakeWhile(c => char.IsWhiteSpace(c)).ToArray());
            int index = position - line.Start;

            char firstChar = text.SkipWhile(Char.IsWhiteSpace).FirstOrDefault();

            if (firstChar == '*' && index > indentation.Length)
            {
                // If first char of the line is * then insert new line and *
                return HandleStartLines(position, indentation);
            }
            else
            {
                // Handles when caret is between /* and */ on the same line
                return HandleBlockComment(position, text, indentation, index);
            }
        }

        private bool HandleBlockComment(int position, string text, string indentation, int index)
        {
            int start = text.IndexOf("/*", StringComparison.Ordinal) + 2;
            int end = text.IndexOf("*/", StringComparison.Ordinal);

            if (start == 1 || end == -1 || index < start || index > end)
                return false;

            string result = Environment.NewLine + indentation + " * " + Environment.NewLine + indentation + " ";

            using (WebEssentialsPackage.UndoContext("Smart Indent"))
            {
                TextView.TextBuffer.Insert(position, result);
                SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, position + indentation.Length + 5);
                TextView.Caret.MoveTo(point);
            }

            return true;
        }

        private bool HandleStartLines(int position, string indentation)
        {
            string result = Environment.NewLine + indentation + "* ";

            using (WebEssentialsPackage.UndoContext("Smart Indent"))
            {
                TextView.TextBuffer.Insert(position, result);
                SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, position + result.Length);
                TextView.Caret.MoveTo(point);
            }

            return true;
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}