using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("SelectorHackSmartTagProvider")]
    internal class SelectorHackSmartTagProvider : ICssSmartTagProvider
    {
        private const string _ie6Only = "* html ";
        private const string _ie7Only = "*:first-child + html ";
        private const string _ie7Above = "html > body ";
        private const string _ie8Above = "html > /**/ body ";

        public Type ItemType
        {
            get { return typeof(Selector); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            var s = (Selector)item;
            if (!s.IsValid)
                yield break;

            if (!s.Text.StartsWith(_ie6Only, StringComparison.Ordinal) &&
                !s.Text.StartsWith(_ie7Only, StringComparison.Ordinal) &&
                !s.Text.StartsWith(_ie7Above, StringComparison.Ordinal) &&
                !s.Text.StartsWith(_ie8Above, StringComparison.Ordinal))
            {
                yield return new SelectorHackSmartTagAction(itemTrackingSpan, s, _ie6Only, Resources.IE6OnlySelectorHackSmartTagActionName);
                yield return new SelectorHackSmartTagAction(itemTrackingSpan, s, _ie7Only, Resources.IE7OnlySelectorHackSmartTagActionName);
                yield return new SelectorHackSmartTagAction(itemTrackingSpan, s, _ie7Above, Resources.IE7AboveSelectorHackSmartTagActionName);
                yield return new SelectorHackSmartTagAction(itemTrackingSpan, s, _ie8Above, Resources.IE8AboveSelectorHackSmartTagActionName);
            }
            else if (s.Text.StartsWith(_ie6Only, StringComparison.Ordinal))
            {
                yield return new RemoveSelectorHackSmartTagAction(itemTrackingSpan, s, _ie6Only);
            }

            else if (s.Text.StartsWith(_ie7Only, StringComparison.Ordinal))
            {
                yield return new RemoveSelectorHackSmartTagAction(itemTrackingSpan, s, _ie7Only);
            }

            else if (s.Text.StartsWith(_ie7Above, StringComparison.Ordinal))
            {
                yield return new RemoveSelectorHackSmartTagAction(itemTrackingSpan, s, _ie7Above);
            }

            else if (s.Text.StartsWith(_ie8Above, StringComparison.Ordinal))
            {
                yield return new RemoveSelectorHackSmartTagAction(itemTrackingSpan, s, _ie8Above);
            }
        }
    }
}
