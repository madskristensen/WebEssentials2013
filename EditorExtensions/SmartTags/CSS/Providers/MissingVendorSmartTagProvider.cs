using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("MissingVendorSmartTagProvider")]
    internal class MissingVendorSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(Declaration); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            var dec = (Declaration)item;

            if (!item.IsValid || position > dec.Colon.Start || dec.IsVendorSpecific())
                yield break;

            switch (dec.PropertyName.Text)
            {
                case "filter":
                case "zoom":
                case "behavior":
                    yield break;
            }

            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);//.GetSchemaRootForBuffer(view.TextBuffer);
            IEnumerable<string> missingEntries = dec.GetMissingVendorSpecifics(schema);

            if (missingEntries.Any())
            {
                var missingPrefixes = missingEntries.Select(e => e.Substring(0, e.IndexOf('-', 1) + 1));
                yield return new VendorSmartTagAction(itemTrackingSpan, dec, missingPrefixes);
            }
        }
    }
}
