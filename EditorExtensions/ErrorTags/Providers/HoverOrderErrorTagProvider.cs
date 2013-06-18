using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("HoverOrderErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class HoverOrderErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (item.Text.TrimStart(':').StartsWith("-"))
                return ItemCheckResult.Continue;

            ParseItem next = item.NextSibling;
            //ParseItem prev = item.PreviousSibling;
            SimpleSelector sel = item.FindType<SimpleSelector>();

            //if (item.Text == ":hover" && prev != null && _invalids.Contains(prev.Text))
            //{
            //    string error = string.Format(Resources.ValidationHoverOrder, prev.Text);
            //    context.AddError(new SimpleErrorTag(item, error, CssErrorFlags.TaskListError | CssErrorFlags.UnderlineRed));
            //}

            if (next != null)
            {
                if (next.Text.StartsWith(":") && item.IsPseudoElement() && !next.IsPseudoElement())
                {
                    string error = string.Format(Resources.ValidationPseudoOrder, item.Text, next.Text);
                    context.AddError(new SimpleErrorTag(item, error, CssErrorFlags.TaskListError | CssErrorFlags.UnderlineRed));
                }

                else if (!next.Text.StartsWith(":") && item.AfterEnd == next.Start)
                {
                    string error = string.Format(Resources.BestPracticePseudosAfterOtherSelectors, next.Text);
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

        private static List<string> _invalids = new List<string>()
        {
            ":before",
            "::before",
            ":after",
            "::after",
        };

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
