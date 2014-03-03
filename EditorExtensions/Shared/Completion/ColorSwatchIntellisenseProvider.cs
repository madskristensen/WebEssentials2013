using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Css.Extensions;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Shared
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("ColorSwatchIntellisenseProvider")]
    internal class ColorSwatchIntellisenseProvider : ICssCompletionListProvider
    {
        readonly static IEnumerable<string> _elegibleTags = new[] { "color", "background-color" };

        public CssCompletionContextType ContextType
        {
            get { return CssCompletionContextType.PropertyValue; }
        }

        public static bool IsColorContext(CssCompletionContext context)
        {
            if (context != null && context.ContextItem != null && context.ContextItem.StyleSheet is CssStyleSheet)
            {
                Declaration dec = context.ContextItem.FindType<Declaration>();
                string propertyName = (dec != null && dec.PropertyName != null) ? dec.PropertyNameText : string.Empty;

                if (_elegibleTags.Contains(propertyName))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            if (!IsColorContext(context))
                yield break;

            StyleSheet styleSheet = context.ContextItem.StyleSheet;
            CssVariableHelpers helper = ((CssStyleSheet)styleSheet).CreateVariableHelpers();
            var declarations = helper.FindDeclaredVariables(styleSheet, context.SpanStart);

            // First collect variables from current documents
            foreach (CssVariableDeclaration color in declarations.Where(c => IsValidColor(c.Value.Text)))
            {
                yield return new ColorSwatchIntellisense(color.VariableName.Text, color.Value.Text);
            }

            // Next, collect from imported documents
            using (MultiDocumentReadLock locks = CssDocumentHelpers.DocumentImportManager.LockImportedStyleSheets(styleSheet))
            {
                foreach (StyleSheet importedStyleSheet in locks.StyleSheets)
                {
                    declarations = helper.FindDeclaredVariables(importedStyleSheet, importedStyleSheet.AfterEnd);

                    foreach (CssVariableDeclaration color in declarations.Where(c => IsValidColor(c.Value.Text)))
                    {
                        yield return new ColorSwatchIntellisense(color.VariableName.Text, color.Value.Text);
                    }
                }
            }
        }

        private static bool IsValidColor(string color)
        {
            color = color.Trim();
            return color.StartsWith("#", StringComparison.Ordinal) ||
                   Regex.IsMatch(color, @"[\/\\*|\-|\+].*#") || // Test case when there is color math involved: like @light-blue: @nice-blue + #111;
                   ColorParser.TryParseColor(color, ColorParser.Options.AllowNames | ColorParser.Options.LooseParsing) != null;
        }
    }
}
