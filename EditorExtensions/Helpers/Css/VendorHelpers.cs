using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.CSS.Editor.SyntaxCheck;

namespace MadsKristensen.EditorExtensions
{
    internal static class VendorHelpers
    {
        private static Dictionary<int, string[]> prefixes = new Dictionary<int, string[]>();

        public static string[] GetPrefixes(ICssSchemaInstance schema)
        {
            int version = schema.Version.GetHashCode();
            int browser = schema.Filter.Name.GetHashCode();
            int hash = version ^ browser;

            if (!prefixes.ContainsKey(hash))
            {
                CssSchemaManager.SchemaManager.CurrentSchemaChanged += CurrentSchemaChanged;
                var properties = schema.Properties;
                List<string> list = new List<string>();

                foreach (ICssCompletionListEntry property in properties)
                {
                    string text = property.DisplayText;
                    if (text[0] == '-')
                    {
                        int end = text.IndexOf('-', 1);
                        if (end > -1)
                        {
                            string prefix = text.Substring(0, end + 1);
                            if (!list.Contains(prefix))
                            {
                                list.Add(prefix);
                            }
                        }
                    }
                }

                prefixes.Add(hash, list.ToArray());
            }

            return prefixes[hash];
        }

        private static void CurrentSchemaChanged(object sender, EventArgs e)
        {
            CssSchemaManager.SchemaManager.CurrentSchemaChanged -= CurrentSchemaChanged;
        }

        public static bool HasVendorLaterInRule(Declaration declaration, ICssSchemaInstance schema)
        {
            Declaration next = declaration.NextSibling as Declaration;

            while (next != null)
            {
                if (next.IsValid && next.IsVendorSpecific())
                {
                    foreach (string prefix in GetPrefixes(schema))
                    {
                        if (next.PropertyName.Text == prefix + declaration.PropertyName.Text)
                            return true;
                    }
                }

                next = next.NextSibling as Declaration;
            }

            return false;
        }

        public static IEnumerable<Declaration> GetMatchingVendorEntriesInRule(Declaration declaration, RuleBlock rule, ICssSchemaInstance schema)
        {
            foreach (Declaration d in rule.Declarations.Where(d => d.IsValid && d.IsVendorSpecific()))
                foreach (string prefix in GetPrefixes(schema))
                {
                    if (d.PropertyName.Text == prefix + declaration.PropertyName.Text)
                    {
                        yield return d;
                        break;
                    }
                }
        }

        public static ICssCompletionListEntry GetMatchingStandardEntry(Declaration declaration, ICssSchemaInstance rootSchema)
        {
            string standardName;
            if (declaration.TryGetStandardPropertyName(out standardName, rootSchema))
            {
                ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, declaration);
                return schema.GetProperty(standardName);
            }

            return null;
        }

        public static ICssCompletionListEntry GetMatchingStandardEntry(Declaration declaration, ICssCheckerContext context)
        {
            string standardName;
            if (declaration.TryGetStandardPropertyName(out standardName, CssEditorChecker.GetSchemaForItem(context, declaration)))
            {
                ICssSchemaInstance schema = CssEditorChecker.GetSchemaForItem(context, declaration);
                return schema.GetProperty(standardName);
            }

            return null;
        }

        public static ICssCompletionListEntry GetMatchingStandardEntry(AtDirective directive, ICssCheckerContext context)
        {
            string standardName;
            if (directive.TryGetStandardPropertyName(out standardName, CssEditorChecker.GetSchemaForItem(context, directive)))
            {
                ICssSchemaInstance schema = CssEditorChecker.GetSchemaForItem(context, directive);
                return schema.GetAtDirective("@" + standardName);
            }

            return null;
        }

        public static ICssCompletionListEntry GetMatchingStandardEntry(AtDirective directive, ICssSchemaInstance rootSchema)
        {
            string standardName;
            if (directive.TryGetStandardPropertyName(out standardName, rootSchema))
            {
                ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, directive);
                return schema.GetAtDirective("@" + standardName);
            }

            return null;
        }
    }
}
