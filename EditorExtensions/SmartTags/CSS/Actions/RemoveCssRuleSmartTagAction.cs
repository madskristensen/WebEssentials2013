using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions.SmartTags.CSS.Actions
{
    internal class RemoveCssRuleSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private RuleSet _rule;
        private int _position;
        
        public RemoveCssRuleSmartTagAction(ITrackingSpan span, int position, RuleSet rule)
        {
            _span = span;
            _rule = rule;
            _position = position;
        }

        public override string DisplayText
        {
            get { return Resources.RemoveUnusedCssRuleSmartTagActionName; }
        }

        public override void Invoke()
        {
            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            var snapshot = _span.TextBuffer.CurrentSnapshot;
            var ss = new SnapshotSpan(snapshot, _rule.Start, _rule.Length);
            _span.TextBuffer.Delete(ss);
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
