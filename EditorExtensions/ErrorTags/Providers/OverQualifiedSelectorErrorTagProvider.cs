using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("OverQualifiedSelectorErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class OverQualifiedSelectorErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            Selector sel = (Selector)item;

            if (!WESettings.GetBoolean(WESettings.Keys.ValidateOverQualifiedSelector) || !sel.IsValid || context == null)
                return ItemCheckResult.Continue;

            int index = sel.Text.IndexOf('#');

            if (index > 0)
            {
                string idName = sel.ItemAfterPosition(sel.Start + index).Text;
                string remove = sel.Text.Substring(0, index);
                string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.PerformanceDontOverQualifySelectors, idName, remove);

                SimpleErrorTag tag = new SimpleErrorTag(sel, errorMessage, index);

                context.AddError(tag);
            }

            return ItemCheckResult.Continue;
        }


        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(Selector) }; }
        }
    }
}
