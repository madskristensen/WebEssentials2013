using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("Ie10PrefixErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class Ie10PrefixErrorTagProvider : ICssItemChecker
    {
        private const string _message = "Validation (WE): {0} no longer applies to Internet Explorer 10. Use the standard implementation instead.";

        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            Declaration dec = (Declaration)item;

            if (context == null || !dec.IsValid)
                return ItemCheckResult.Continue;

            string text = dec.PropertyName.Text;

            if (text.StartsWith("-ms-transition", StringComparison.Ordinal) || text.StartsWith("-ms-animation", StringComparison.Ordinal))
            {
                string error = string.Format(CultureInfo.CurrentCulture, _message, text);
                ICssError tag = new SimpleErrorTag(dec.PropertyName, error);
                context.AddError(tag);
                return ItemCheckResult.CancelCurrentItem;
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(Declaration) }; }
        }
    }
}
