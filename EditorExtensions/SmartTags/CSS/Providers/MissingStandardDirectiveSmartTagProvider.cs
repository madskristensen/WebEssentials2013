using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("MissingStandardDirectiveSmartTagProvider")]
    internal class MissingStandardDirectiveSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(AtDirective); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            AtDirective directive = (AtDirective)item;

            if (!item.IsValid || !directive.IsVendorSpecific())
                yield break;

            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaRootForBuffer(view.TextBuffer);
            var visitor = new CssItemCollector<AtDirective>();
            directive.Parent.Accept(visitor);

            ICssCompletionListEntry entry = VendorHelpers.GetMatchingStandardEntry(directive, schema);
            if (entry != null && !visitor.Items.Any(d => d.Keyword != null && "@" + d.Keyword.Text == entry.DisplayText))
            {
                yield return new MissingStandardDirectiveSmartTagAction(itemTrackingSpan, directive, entry.DisplayText);
            }
        }
    }
}
