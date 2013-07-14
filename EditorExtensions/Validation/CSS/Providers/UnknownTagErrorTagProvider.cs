using Microsoft.CSS.Core;
using Microsoft.Html.Schemas;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("UnknownTagErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class UnknownTagErrorTagProvider : ICssItemChecker
    {
        private HashSet<string> _cache = new HashSet<string>(BuildCache());
          

        private static IEnumerable<string> BuildCache()
        {
            yield return "*";

            HtmlSchemaManager mng = new HtmlSchemaManager();
            IHtmlSchema schema = mng.GetSchema("http://schemas.microsoft.com/intellisense/html");

            foreach (var element in schema.GetTopLevelElements())
            {
                yield return element.Name;
            }
        }

        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            ItemName itemName = (ItemName)item;

            if (!itemName.IsValid || context == null || (item.PreviousSibling != null && item.PreviousSibling.Text == "["))
                return ItemCheckResult.Continue;

            if (!_cache.Contains(itemName.Text.ToLowerInvariant()) && itemName.Text.IndexOf('-') == -1)
            {
                string error = "Validation: \"" + itemName.Text + "\" isn't a valid HTML tag.";
                ICssError tag = new SimpleErrorTag(itemName, error);
                context.AddError(tag);

                return ItemCheckResult.CancelCurrentItem;
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(ItemName) }; }
        }
    }
}
