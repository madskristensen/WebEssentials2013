using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("HoverOrderErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class HoverOrderErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (item.Text.TrimStart(':').StartsWith("-", StringComparison.Ordinal))
                return ItemCheckResult.Continue;

            ParseItem next = item.NextSibling;
            if (next != null)
            {
                if (next.Text.StartsWith(":", StringComparison.Ordinal) && item.IsPseudoElement() && !next.IsPseudoElement())
                {
                    string error = string.Format(CultureInfo.CurrentCulture, Resources.ValidationPseudoOrder, item.Text, next.Text);
                    context.AddError(new SimpleErrorTag(item, error, CssErrorFlags.TaskListError | CssErrorFlags.UnderlineRed));
                }
                else if (!next.Text.StartsWith(":", StringComparison.Ordinal) && item.AfterEnd == next.Start)
                {
                    string error = string.Format(CultureInfo.CurrentCulture, Resources.BestPracticePseudosAfterOtherSelectors, next.Text);
                    context.AddError(new SimpleErrorTag(next, error));
                }
            }

            return ItemCheckResult.Continue;
        }

        //public static bool IsPseudoElement(ParseItem item)
        //{
        //    if (item.Text.StartsWith("::"))
        //        return true;

        //    var schema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
        //    return schema.GetPseudo(":" + item.Text) != null;
        //}

        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new[] 
                { 
                    typeof(PseudoClassSelector),
                    typeof(PseudoClassFunctionSelector),
                    typeof(PseudoElementFunctionSelector),
                    typeof(PseudoElementSelector),
                };
            }
        }
    }
}
