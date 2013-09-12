using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.SmartTags.CSS.Actions
{
    internal class RemoveCssRuleSmartTagAction : CssSmartTagActionBase
    {
        private readonly ITrackingSpan _span;
        private readonly RuleSet _rule;

        public RemoveCssRuleSmartTagAction(ITrackingSpan span, RuleSet rule)
        {
            _span = span;
            _rule = rule;
        }

        public override string DisplayText
        {
            get { return Resources.RemoveUnusedCssRuleSmartTagActionName; }
        }

        public override void Invoke()
        {
            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            var snapshot = _span.TextBuffer.CurrentSnapshot;
            var position = _rule.Start + _rule.Length;
            var end = CalculateEndPositionToTrimLineEndings(snapshot, position);
            var ss = new SnapshotSpan(snapshot, _rule.Start, end - _rule.Start);
            _span.TextBuffer.Delete(ss);

            EditorExtensionsPackage.DTE.UndoContext.Close();
        }

        private static int CalculateEndPositionToTrimLineEndings(ITextSnapshot snapshot, int startPosition)
        {
            var position = startPosition;

            while (true)
            {
                if (position < snapshot.Length)
                {
                    var ss = new SnapshotSpan(snapshot, position, 1);

                    if (string.IsNullOrWhiteSpace(ss.GetText()))
                    {
                        ++position;
                        continue;
                    }
                }

                return position;
            }
        }
    }
}
