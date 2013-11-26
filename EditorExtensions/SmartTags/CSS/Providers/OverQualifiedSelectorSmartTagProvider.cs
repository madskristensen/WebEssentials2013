using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("OverQualifiedSelectorSmartTagProvider")]
    [Order(Before = "AlphabetizeSmartTagProvider")]
    internal class OverQualifiedSelectorSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(Selector); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            Selector sel = (Selector)item;
            if (sel == null || !sel.IsValid)
                yield break;

            var idPart = sel.SimpleSelectors.Skip(1).FirstOrDefault(s => s.Text.StartsWith("#"));
            if (idPart != null)
            {
                yield return new OverQualifySelectorSmartTagAction(sel, itemTrackingSpan, idPart.Start - sel.Start);
            }
        }
    }
}
