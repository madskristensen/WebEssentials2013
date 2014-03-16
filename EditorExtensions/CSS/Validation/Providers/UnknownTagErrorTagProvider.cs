using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.Html.Schemas;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssItemChecker))]
    [Name("UnknownTagErrorTagProvider")]
    [Order(After = "Default Declaration")]
    public class UnknownTagErrorTagProvider : ICssItemChecker
    {
        private HashSet<string> _cache = new HashSet<string>(BuildCache());
        private static HashSet<string> _basicIgnore = new HashSet<string>() { "noscript", "font", "___all___", "FlowContentElement" };
        private static HashSet<string> _cssIgnore = new HashSet<string>(_basicIgnore) { "script", "meta", "link", "style", "head", };

        private static IEnumerable<string> BuildCache()
        {
            yield return "*";

            foreach (var element in GetListEntriesCache(includeUnstyled: true))
            {
                yield return element.DisplayText;
            }
        }

        public static IEnumerable<ICssCompletionListEntry> GetListEntriesCache(bool includeUnstyled)
        {
            var ignoreList = includeUnstyled ? _cssIgnore : _basicIgnore;

            foreach (var schema in Schemas)
                foreach (var element in schema.GetTopLevelElements())
                {
                    if (!ignoreList.Contains(element.Name))
                        yield return new CompletionListEntry(element.Name) { Description = element.Description.Description };

                    foreach (var child in element.GetChildren())
                    {
                        if (!ignoreList.Contains(child.Name))
                            yield return new CompletionListEntry(child.Name) { Description = child.Description.Description };
                    }
                }
        }

        public static IEnumerable<IHtmlSchema> Schemas
        {
            get
            {
                HtmlSchemaManager mng = new HtmlSchemaManager();
                IHtmlSchema html = mng.GetSchema("http://schemas.microsoft.com/intellisense/html");

                var schemas = mng.CustomAttributePrefixes.SelectMany(p => mng.GetSupplementalSchemas(p)).ToList();
                schemas.Insert(0, html);

                return schemas;
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
