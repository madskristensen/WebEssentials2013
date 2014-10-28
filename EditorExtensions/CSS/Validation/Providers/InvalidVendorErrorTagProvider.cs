using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssItemChecker))]
    [Name("InvalidVendorDeclarationErrorTagProvider")]
    [Order(After = "Ie10PrefixErrorTagProvider")]
    internal class InvalidVendorDeclarationErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (!WESettings.Instance.Css.ValidateVendorSpecifics)
                return ItemCheckResult.Continue;

            Declaration dec = (Declaration)item;

            if (!dec.IsValid || !dec.IsVendorSpecific() || context == null)
                return ItemCheckResult.Continue;

            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, item);

            ICssCompletionListEntry prop = schema.GetProperty(dec.PropertyName.Text);

            if (prop == null)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.ValidationVendorDeclarations, dec.PropertyName.Text);
                context.AddError(new SimpleErrorTag(dec.PropertyName, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                return ItemCheckResult.CancelCurrentItem;
            }
            else
            {
                string obsolete = prop.GetAttribute("obsolete");
                if (!string.IsNullOrEmpty(obsolete))
                {
                    string message = string.Format(CultureInfo.CurrentCulture, Resources.BestPracticeRemoveObsolete, dec.PropertyName.Text, obsolete);
                    context.AddError(new SimpleErrorTag(dec.PropertyName, message, CssErrorFlags.TaskListMessage));
                    return ItemCheckResult.CancelCurrentItem;
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(Declaration) }; }
        }
    }

    [Export(typeof(ICssItemChecker))]
    [Name("InvalidVendorPseudoErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class InvalidVendorPseudoErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (!WESettings.Instance.Css.ValidateVendorSpecifics)
                return ItemCheckResult.Continue;

            if (!item.IsValid || context == null)
                return ItemCheckResult.Continue;

            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, item);

            string normalized = item.Text.Trim(':');
            ICssCompletionListEntry pseudo = schema.GetPseudo(item.Text);

            if (normalized.Length > 0 && normalized[0] == '-' && pseudo == null)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.ValidationVendorPseudo, item.Text);
                context.AddError(new SimpleErrorTag(item, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                return ItemCheckResult.CancelCurrentItem;
            }
            else if (pseudo != null)
            {
                string obsolete = pseudo.GetAttribute("obsolete");
                if (!string.IsNullOrEmpty(obsolete))
                {
                    string message = string.Format(CultureInfo.CurrentCulture, Resources.BestPracticeRemoveObsolete, normalized, obsolete);
                    context.AddError(new SimpleErrorTag(item, message, CssErrorFlags.TaskListMessage));
                    return ItemCheckResult.CancelCurrentItem;
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(PseudoClassSelector), typeof(PseudoElementSelector) }; }
        }
    }

    [Export(typeof(ICssItemChecker))]
    [Name("InvalidVendorDirectiveErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class InvalidVendorDirectiveErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (!WESettings.Instance.Css.ValidateVendorSpecifics)
                return ItemCheckResult.Continue;

            AtDirective dir = item as AtDirective;

            if (!dir.IsValid || !dir.IsVendorSpecific() || context == null)
                return ItemCheckResult.Continue;


            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, dir);

            ICssCompletionListEntry at = schema.GetAtDirective("@" + dir.Keyword.Text);

            if (at == null)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.ValidationVendorDirective, dir.Keyword.Text);
                context.AddError(new SimpleErrorTag(dir.Keyword, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                return ItemCheckResult.CancelCurrentItem;
            }
            else
            {
                string obsolete = at.GetAttribute("obsolete");
                if (!string.IsNullOrEmpty(obsolete))
                {
                    string message = string.Format(CultureInfo.CurrentCulture, Resources.BestPracticeRemoveObsolete, "@" + dir.Keyword.Text, obsolete);
                    context.AddError(new SimpleErrorTag(dir.Keyword, message, CssErrorFlags.TaskListMessage));
                    return ItemCheckResult.CancelCurrentItem;
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(AtDirective) }; }
        }
    }
}