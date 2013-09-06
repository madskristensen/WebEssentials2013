using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using System;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions.SmartTags.CSS.Actions
{
    internal class RemoveCssRuleSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private RuleSet _rule;

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
            _span.TextBuffer.Delete(_span.GetSpan(_span.TextBuffer.CurrentSnapshot));
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
