using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

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
            
            int index = sel.Text.IndexOf('#');
            if (index > 0)
            {
                yield  return new OverQualifySelectorSmartTagAction(sel, itemTrackingSpan, index);
            }
        }
    }
}
