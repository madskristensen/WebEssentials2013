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
            if (!url.IsValid || url.UrlString == null || string.IsNullOrEmpty(url.UrlString.Text))
                yield break;

            string text = url.UrlString.Text.Trim('"', '\'');
            string testSupportFileName = text;

            if (url.IsDataUri())
            {
                string mime = FileHelpers.GetMimeTypeFromBase64(text);
                testSupportFileName = "file." + FileHelpers.GetExtension(mime);
            }

            if (!ImageCompressor.IsFileSupported(testSupportFileName))
                yield break;

            yield return new OptimizeImageSmartTagAction(itemTrackingSpan, url);
        }
    }
}
