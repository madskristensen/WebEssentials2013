using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;

namespace MadsKristensen.EditorExtensions
{
    internal static class AtDirectiveExtensions
    {
        public static bool IsVendorSpecific(this AtDirective directive)
        {
            return directive.Keyword.Text[0] == '-';
        }


        public static bool TryGetStandardPropertyName(this AtDirective directive, out string standardName, ICssSchemaInstance schema)
        {
            standardName = null;

            string propText = directive.Keyword.Text;
            string prefix = VendorHelpers.GetPrefixes(schema).SingleOrDefault(p => propText.StartsWith(p, StringComparison.Ordinal));
            if (prefix != null)
            {
                standardName = propText.Substring(prefix.Length);
                return true;
            }

            return false;
        }

        public static IEnumerable<string> GetMissingVendorSpecifics(this AtDirective directive, ICssSchemaInstance schema)
        {
            IEnumerable<string> possible = GetPossibleVendorSpecifics(directive, schema);

            var visitorRules = new CssItemCollector<AtDirective>();
            directive.Parent.Accept(visitorRules);

            foreach (string item in possible)
            {
                if (!visitorRules.Items.Any(d => d.Keyword != null && "@" + d.Keyword.Text == item))
                    yield return item;
            }
        }

        public static IEnumerable<string> GetPossibleVendorSpecifics(this AtDirective directive, ICssSchemaInstance schema)
        {
            string text = directive.Keyword.Text;

            foreach (string prefix in VendorHelpers.GetPrefixes(schema).Where(p => p != "-o-")) // Remove -o- since the parser doesn't recognize -o-keyframes
            {
                ICssCompletionListEntry entry = schema.GetAtDirective("@" + prefix + text);
                if (entry != null)
                    yield return entry.DisplayText;
            }
        }
    }
}
