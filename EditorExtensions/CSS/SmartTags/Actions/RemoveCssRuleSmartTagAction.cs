using System;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class RemoveCssRuleSmartTagAction : CssSmartTagActionBase
    {
        private readonly ITrackingSpan _span;
        private readonly RuleSet _rule;

        public RemoveCssRuleSmartTagAction(ITrackingSpan span, RuleSet rule)
        {
            _span = span;
            _rule = rule;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/delete.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.RemoveUnusedCssRuleSmartTagActionName; }
        }

        public override void Invoke()
        {
            using (WebEssentialsPackage.UndoContext((DisplayText)))
            {
                var snapshot = _span.TextBuffer.CurrentSnapshot;
                var position = _rule.Start + _rule.Length;
                var start = CalculateDeletionStartFromStartPosition(snapshot, _rule.Start);
                var end = CalculateDeletionEndFromRuleEndPosition(snapshot, position);
                var length = end - start;
                var ss = new SnapshotSpan(snapshot, start, length);
                _span.TextBuffer.Delete(ss);
            }
        }

        private static int CalculateDeletionStartFromStartPosition(ITextSnapshot snapshot, int startPosition)
        {
            var position = startPosition - 1;

            if (position < 0)
            {
                return 0;
            }

            while (true)
            {
                if (position > 0)
                {
                    var ss = new SnapshotSpan(snapshot, position, 1);
                    var text = ss.GetText();

                    if (text != null && !"\r\n".Contains(text) && string.IsNullOrWhiteSpace(text))
                    {
                        --position;
                        continue;
                    }

                    ++position;
                }

                return position;
            }
        }

        private static int CalculateDeletionEndFromRuleEndPosition(ITextSnapshot snapshot, int endPosition)
        {
            var position = endPosition;
            var committedPosition = position;

            while (true)
            {
                if (position < snapshot.Length)
                {
                    var ss = new SnapshotSpan(snapshot, position, 1);
                    var text = ss.GetText();

                    if (text != null)
                    {
                        if ("\r\n".Contains(text))
                        {
                            committedPosition = ++position;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            ++position;
                            continue;
                        }
                    }
                }

                return committedPosition;
            }
        }
    }
}
