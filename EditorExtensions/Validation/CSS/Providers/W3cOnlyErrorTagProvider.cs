using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("W3cOnlyErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class W3cOnlyErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.OnlyW3cAllowed))
                return ItemCheckResult.Continue;

            if (item is Declaration)
            {
                HandleDeclaration(item, context);
            }
            else if (item is AtDirective)
            {
                HandleDirective(item, context);
            }

            else if (item is PseudoClassFunctionSelector || item is PseudoClassSelector || item is PseudoElementFunctionSelector || item is PseudoElementSelector)
            {
                HandlePseudo(item, context);
            }

            return ItemCheckResult.Continue;
        }

        private static void HandleDeclaration(ParseItem item, ICssCheckerContext context)
        {
            Declaration dec = (Declaration)item;

            if (dec == null || dec.PropertyName == null)
                return;

            if (dec.IsVendorSpecific())
            {
                string message = string.Format("Validation (W3C): \"{0}\" is not a valid W3C property", dec.PropertyName.Text);
                context.AddError(new SimpleErrorTag(dec.PropertyName, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
            }

            foreach (var value in dec.Values)
            {
                string text = value.Text;
                if (!(value is NumericalValue) && text.StartsWith("-", StringComparison.Ordinal))
                {
                    int index = text.IndexOf('(');

                    if (index > -1)
                    {
                        text = text.Substring(0, index);
                    }

                    string message = string.Format("Validation (W3C): \"{0}\" is not a valid W3C value", text);
                    context.AddError(new SimpleErrorTag(value, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                }
            }
        }

        private static void HandleDirective(ParseItem item, ICssCheckerContext context)
        {
            AtDirective dir = (AtDirective)item;

            if (dir == null || dir.Keyword == null)
                return;

            if (dir.IsVendorSpecific())
            {
                string message = string.Format("Validation (W3C): \"@{0}\" is not a valid W3C @-directive", dir.Keyword.Text);
                context.AddError(new SimpleErrorTag(dir.Keyword, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
            }
        }

        private static void HandlePseudo(ParseItem item, ICssCheckerContext context)
        {
            string text = item.Text.TrimStart(':');

            if (text.StartsWith("-", StringComparison.Ordinal))
            {
                string message = string.Format("Validation (W3C): \"{0}\" is not a valid W3C pseudo class/element", item.Text);
                context.AddError(new SimpleErrorTag(item, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
            }
        }

        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new[] 
                { 
                    typeof(Declaration), 
                    typeof(AtDirective),
                    typeof(PseudoClassFunctionSelector),
                    typeof(PseudoClassSelector),
                    typeof(PseudoElementFunctionSelector),
                    typeof(PseudoElementSelector),
                };
            }
        }
    }
}