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
    [Name("MissingVendorDirectiveSmartTagProvider")]
    internal class MissingVendorDirectiveSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(AtDirective); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            var directive = (AtDirective)item;

            if (!item.IsValid || directive.IsVendorSpecific())
                yield break;

            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);//.GetSchemaRootForBuffer(view.TextBuffer);
            IEnumerable<string> missingEntries = directive.GetMissingVendorSpecifics(schema);

            if (missingEntries.Any())
            {
                yield return new VendorDirectiveSmartTagAction(itemTrackingSpan, directive, missingEntries);
            }
        }
    }
}
