﻿using Microsoft.CSS.Core;
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
            var start = CalculateDeletionStartFromStartPosition(snapshot, _rule.Start);
            var end = CalculateDeletionEndFromRuleEndPosition(snapshot, position);
            var length = end - start;
            var ss = new SnapshotSpan(snapshot, start, length);
            _span.TextBuffer.Delete(ss);

            EditorExtensionsPackage.DTE.UndoContext.Close();
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
