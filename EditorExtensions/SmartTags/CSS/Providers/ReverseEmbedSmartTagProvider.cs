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
    [Name("ReverseEmbedSmartTagProvider")]
    internal class ReverseEmbedSmartTagProvider : ICssSmartTagProvider
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

            if (url.UrlString.Text.Contains(";base64,"))
            {
                yield return new ReverseEmbedSmartTagAction(itemTrackingSpan, url);
            }
        }
    }
}
