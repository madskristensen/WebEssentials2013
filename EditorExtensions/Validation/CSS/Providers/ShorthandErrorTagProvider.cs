using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("ShorthandErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class ShorthandErrorTagProvider : ICssItemChecker
    {
        private static Dictionary<string, string[]> _cache = new Dictionary<string, string[]>()
        {
            {"margin", new [] { "margin-top", "margin-right", "margin-bottom", "margin-left" }},
            {"padding", new [] { "padding-top", "padding-right", "padding-bottom", "padding-left" }},
            {"border", new [] { "border-width", "border-style", "border-color" }},
            {"border-color", new [] { "border-left-color", "border-top-color", "border-right-color", "border-bottom-color" }},
            {"border-style", new [] { "border-left-style", "border-top-style", "border-right-style", "border-bottom-style" }},
            {"border-radius", new [] { "border-top-left-radius", "border-top-right-radius", "border-bottom-left-radius", "border-bottom-right-radius" }},
            {"outline", new [] { "outline-width", "outline-style", "outline-color" }},
            {"list-style", new [] { "list-style-type", "list-style-position", "list-style-image" }},
            {"text-decoration", new [] { "text-decoration-color", "text-decoration-style", "text-decoration-line" }},
        };


        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            RuleBlock rule = (RuleBlock)item;

            if (!rule.IsValid || context == null)
                return ItemCheckResult.Continue;

            IEnumerable<string> properties = from d in rule.Declarations
                                             where d.PropertyName != null && d.Values.Count < 2
                                             select d.PropertyName.Text;


            foreach (string shorthand in _cache.Keys)
            {
                if (_cache[shorthand].All(p => properties.Contains(p)))
                {
                    Declaration dec = rule.Declarations.First(p => p.PropertyName != null && _cache[shorthand].Contains(p.PropertyName.Text));
                    string message = string.Format(CultureInfo.CurrentCulture, Resources.PerformanceUseShorthand, string.Join(", ", _cache[shorthand]), shorthand);

                    context.AddError(new SimpleErrorTag(dec, message));
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(RuleBlock) }; }
        }
    }
}