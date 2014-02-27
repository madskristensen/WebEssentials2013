using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.CSS.Editor.SyntaxCheck;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("VendorOrderErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class VendorOrderErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            Declaration dec = (Declaration)item;

            if (context == null || !dec.IsValid)
                return ItemCheckResult.Continue;

            RuleBlock rule = dec.FindType<RuleBlock>();
            if (!rule.IsValid)
                return ItemCheckResult.Continue;

            if (!dec.IsVendorSpecific())
            {
                ICssSchemaInstance schema = CssEditorChecker.GetSchemaForItem(context, item);
                bool hasVendor = VendorHelpers.HasVendorLaterInRule(dec, schema);
                if (hasVendor)
                {
                    context.AddError(new SimpleErrorTag(dec.PropertyName, Resources.BestPracticeStandardPropertyOrder));
                    return ItemCheckResult.CancelCurrentItem;
                }
            }
            else
            {
                ICssCompletionListEntry entry = VendorHelpers.GetMatchingStandardEntry(dec, context);
                if (entry != null && !rule.Declarations.Any(d => d.PropertyName != null && d.PropertyName.Text == entry.DisplayText))
                {
                    if (entry.DisplayText != "filter" && entry.DisplayText != "zoom" && entry.DisplayText != "behavior")
                    {
                        string message = string.Format(CultureInfo.InvariantCulture, Resources.BestPracticeAddMissingStandardProperty, entry.DisplayText);
                        context.AddError(new SimpleErrorTag(dec.PropertyName, message));
                        return ItemCheckResult.CancelCurrentItem;
                    }
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(Declaration) }; }
        }
    }
}
