//using Microsoft.CSS.Core;
//using Microsoft.CSS.Editor;
//using Microsoft.VisualStudio.Utilities;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Linq;

//namespace MadsKristensen.EditorExtensions
//{
//    [Export(typeof(ICssItemChecker))]
//    [Name("MissingVendorPseudoErrorTagProvider")]
//    [Order(After = "Default Declaration")]
//    internal class MissingVendorPseudoErrorTagProvider : ICssItemChecker
//    {
//        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
//        {
//            string name = item.Text.TrimStart(':');
//            RuleSet rule = item.FindType<RuleSet>();
//            var buffer = ProjectHelpers.GetCurentTextBuffer();

//            if (rule == null || buffer == null)
//                return ItemCheckResult.Continue;

//            ICssSchemaInstance root = CssSchemaManager.SchemaManager.GetSchemaRootForBuffer(buffer);
//            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(root, item);
//            string selector = GetSelectorText(rule);
//            string standardName = StandardizeName(name);

//            //if (standardName == "selection" || standardName == "progress-bar")
//            //    return ItemCheckResult.Continue;

//            IEnumerable<string> missingPseudos = FindMissingPseudos(standardName, selector, schema).Where(p => p != item.Text);

//            if (missingPseudos.Any())
//            {
//                string message = string.Format("Browser compatibility: Add selector to the rule with missing pseudo element/class ({0})", string.Join(", ", missingPseudos));
//                context.AddError(new SimpleErrorTag(item, message));
//            }

//            return ItemCheckResult.Continue;
//        }

//        public static string StandardizeName(string name)
//        {
//            if (name.StartsWith("-"))
//            {
//                int index = name.IndexOf('-', 1);
//                if (index > -1)
//                    return name.Substring(index + 1);
//            }

//            return name;
//        }

//        public static IEnumerable<string> FindMissingPseudos(string name, string selector, ICssSchemaInstance schema)
//        {
//            ICssCompletionListEntry standard = FindPseudo(schema, name);

//            if (standard != null && !selector.Contains(standard.DisplayText))
//                yield return standard.DisplayText;

//            foreach (string prefix in VendorHelpers.GetPrefixes(schema))
//            {
//                string text = GetPseudoName(prefix, name);
//                ICssCompletionListEntry pseudo = FindPseudo(schema, text);

//                if (pseudo != null)
//                {
//                    if (!selector.Contains(pseudo.DisplayText))
//                        yield return pseudo.DisplayText;
//                }
//            }
//        }

//        private static ICssCompletionListEntry FindPseudo(ICssSchemaInstance schema, string text)
//        {
//            ICssCompletionListEntry pseudo = schema.GetPseudo(":" + text);

//            if (pseudo == null)
//            {
//                pseudo = schema.GetPseudo("::" + text);
//            }

//            return pseudo;
//        }

//        private static string GetPseudoName(string prefix, string name)
//        {
//            if (prefix == "-moz-")
//            {
//                if (name.EndsWith("input-placeholder"))
//                    return "-moz-placeholder";
//            }
//            else
//            {
//                if (name == "placeholder")
//                    return prefix + "input-placeholder";
//            }

//            return prefix + name;
//        }

//        public static string GetSelectorText(RuleSet rule)
//        {
//            IEnumerable<string> text = rule.Selectors.Select(s => s.Text);

//            return string.Join(",", text);
//        }

//        public IEnumerable<Type> ItemTypes
//        {
//            get { return new[] { typeof(PseudoClassSelector) }; }
//        }
//    }
//}