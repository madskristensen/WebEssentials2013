using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using MadsKristensen.EditorExtensions.SmartTags.CSS.Actions;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.SmartTags.CSS.Providers
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("RemoveCssRuleSmartTagProvider")]
    internal class RemoveCssRuleSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(Selector); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            RuleSet rule = item.FindType<RuleSet>();
            if (rule == null || rule.Block == null || !rule.Block.IsValid || UsageRegistry.IsRuleUsed(rule))
                yield break;

            yield return new RemoveCssRuleSmartTagAction(itemTrackingSpan, rule);
        }
    }
}
