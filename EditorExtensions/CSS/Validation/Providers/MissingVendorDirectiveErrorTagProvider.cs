using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.CSS.Editor.SyntaxCheck;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("MissingVendorDirectiveErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class MissingVendorDirectiveErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            AtDirective directive = (AtDirective)item;

            if (!directive.IsValid || directive.IsVendorSpecific() || context == null)
                return ItemCheckResult.Continue;

            ICssSchemaInstance schema = CssEditorChecker.GetSchemaForItem(context, item);
            var missingEntries = directive.GetMissingVendorSpecifics(schema);

            if (missingEntries.Any())
            {
                string error = string.Format(CultureInfo.InvariantCulture, Resources.BestPracticeAddMissingVendorSpecificDirective, directive.Keyword.Text, string.Join(", ", missingEntries));
                ICssError tag = new SimpleErrorTag(directive.Keyword, error);
                context.AddError(tag);
                return ItemCheckResult.CancelCurrentItem;
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(AtDirective) }; }
        }
    }
}
