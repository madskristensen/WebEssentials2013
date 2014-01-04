using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;

namespace MadsKristensen.EditorExtensions
{
    internal static class DeclarationExtensions
    {
        public static bool IsVendorSpecific(this Declaration declaration)
        {
            return declaration.PropertyName.Length > 0 ? declaration.PropertyName.Text[0] == '-' : false;
        }

        public static bool TryGetStandardPropertyName(this Declaration declaration, out string standardName, ICssSchemaInstance schema)
        {
            standardName = null;

            if (declaration.IsVendorSpecific())
            {
                string propText = declaration.PropertyName.Text;
                string prefix = VendorHelpers.GetPrefixes(schema).SingleOrDefault(p => propText.StartsWith(p, StringComparison.Ordinal));
                if (prefix != null)
                {
                    standardName = propText.Substring(prefix.Length);
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<string> GetMissingVendorSpecifics(this Declaration declaration, ICssSchemaInstance schema)
        {
            RuleBlock rule = declaration.FindType<RuleBlock>();
            IEnumerable<string> possible = GetPossibleVendorSpecifics(declaration, schema);

            foreach (string item in possible)
            {
                if (!rule.Declarations.Any(d => d.PropertyName != null && d.PropertyName.Text == item))
                    yield return item;
            }
        }

        public static IEnumerable<string> GetPossibleVendorSpecifics(this Declaration declaration, ICssSchemaInstance schema)
        {
            string text = declaration.PropertyName.Text;

            foreach (string prefix in VendorHelpers.GetPrefixes(schema))
            {
                ICssCompletionListEntry entry = schema.GetProperty(prefix + text);
                if (entry != null)
                    yield return entry.DisplayText;
            }
        }
    }
}
