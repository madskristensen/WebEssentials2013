﻿using System;
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

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("VendorOrderSmartTagProvider")]
    internal class VendorOrderSmartTagProvider : ICssSmartTagProvider
    {
        private ICssSchemaInstance _schema;

        public Type ItemType
        {
            get { return typeof(Declaration); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            Declaration dec = (Declaration)item;

            if (!item.IsValid || position > dec.Colon.Start || !EnsureInitialized(itemTrackingSpan.TextBuffer))
                yield break;

            RuleBlock rule = dec.FindType<RuleBlock>();
            if (!rule.IsValid)
                yield break;

            if (!dec.IsVendorSpecific())
            {
                IEnumerable<Declaration> vendors = VendorHelpers.GetMatchingVendorEntriesInRule(dec, rule, _schema);
                if (vendors.Any(v => v.Start > dec.Start))
                {
                    yield return new VendorOrderSmartTagAction(itemTrackingSpan, vendors.Last(), dec);
                }
            }
            else
            {
                ICssCompletionListEntry entry = VendorHelpers.GetMatchingStandardEntry(dec, _schema);
                if (entry != null && !rule.Declarations.Any(d => d.PropertyName != null && d.PropertyName.Text == entry.DisplayText))
                {
                    yield return new MissingStandardSmartTagAction(itemTrackingSpan, dec, entry.DisplayText);
                }
            }
        }

        public bool EnsureInitialized(ITextBuffer buffer)
        {
            if (_schema == null)
            {
                try
                {
                    _schema = CssSchemaManager.SchemaManager.GetSchemaRootForBuffer(buffer);
                }
                catch (Exception)
                {
                }
            }

            return _schema != null;
        }
    }
}
