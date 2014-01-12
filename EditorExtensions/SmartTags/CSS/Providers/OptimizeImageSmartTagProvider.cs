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
    [Name("OptimizeImageSmartTagProvider")]
    internal class OptimizeImageSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(UrlItem); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            UrlItem url = (UrlItem)item;
            if (!url.IsValid || url.UrlString == null)
                yield break;

            yield return new OptimizeImageSmartTagAction(itemTrackingSpan, url);
        }
    }
}
