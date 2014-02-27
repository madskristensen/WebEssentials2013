using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("StarSelectorErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class StarSelectorErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            SimpleSelector sel = (SimpleSelector)item;

            if (!WESettings.Instance.Css.ValidateStarSelector || !sel.IsValid || context == null)
                return ItemCheckResult.Continue;

            if (sel.Text == "*")
            {
                //string afterStar = sel.Text.Length > index + 1 ? sel.Text.Substring(index + 1) : null;
                //if (afterStar == null || !afterStar.Trim().StartsWith("html", StringComparison.OrdinalIgnoreCase))
                //{
                string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.PerformanceDontUseStarSelector);

                SimpleErrorTag tag = new SimpleErrorTag(sel, errorMessage);

                context.AddError(tag);
                //}
            }

            return ItemCheckResult.Continue;
        }


        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(SimpleSelector) }; }
        }
    }
}
