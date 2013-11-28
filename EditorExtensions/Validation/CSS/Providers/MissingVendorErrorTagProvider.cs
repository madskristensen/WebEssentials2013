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
    [Name("MissingVendorErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class MissingVendorErrorTagProvider : ICssItemChecker
    {
        private static string[] _vendorIgnoreList = new[] { "filter", "zoom", "behavior" };

        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            Declaration dec = (Declaration)item;

            if (!dec.IsValid || dec.IsVendorSpecific() || IgnoreProperty(dec) || context == null)
                return ItemCheckResult.Continue;

            ICssSchemaInstance schema = CssEditorChecker.GetSchemaForItem(context, item);
            var missingEntries = dec.GetMissingVendorSpecifics(schema);

            if (missingEntries.ToArray().Length > 0)
            {
                var missingPrefixes = missingEntries.Select(e => e.Substring(0, e.IndexOf('-', 1) + 1));
                string error = string.Format(CultureInfo.InvariantCulture, Resources.BestPracticeAddMissingVendorSpecific, dec.PropertyName.Text, string.Join(", ", missingPrefixes));
                ICssError tag = new SimpleErrorTag(dec.PropertyName, error);
                context.AddError(tag);
                return ItemCheckResult.CancelCurrentItem;
            }

            return ItemCheckResult.Continue;
        }

        private static bool IgnoreProperty(Declaration declaration)
        {
            return _vendorIgnoreList.Contains(declaration.PropertyName.Text);
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(Declaration) }; }
        }
    }
}
