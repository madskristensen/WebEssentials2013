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
    [Name("UrlSmartTagProvider")]
    internal class EmbedSmartTagProvider : ICssSmartTagProvider
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

            Declaration dec = url.FindType<Declaration>();
            if (dec != null && (dec.PropertyName == null || dec.PropertyName.Text.StartsWith("*")))
                yield break;

            if (!url.UrlString.Text.Contains(";base64,") && !url.UrlString.Text.Contains("://"))
            {
                yield return new EmbedSmartTagAction(itemTrackingSpan, url);
            }
        }
    }
}
