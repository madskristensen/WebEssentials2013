using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("InvalidVendorDeclarationErrorTagProvider")]
    [Order(After = "Ie10PrefixErrorTagProvider")]
    internal class InvalidVendorDeclarationErrorTagProvider : ICssItemChecker
    {
        //private HashSet<string> _deprecated = new HashSet<string>()
        //{
        //    "-moz-opacity",
        //    "-moz-outline",
        //    "-moz-outline-style",
        //};

        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (!WESettings.Instance.Css.ValidateVendorSpecifics)
                return ItemCheckResult.Continue;

            Declaration dec = (Declaration)item;

            if (!dec.IsValid || !dec.IsVendorSpecific() || context == null)
                return ItemCheckResult.Continue;

            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, item);

            //if (_deprecated.Contains(dec.PropertyName.Text))
            //{
            //    string message = string.Format(Resources.ValidationDeprecatedVendorDeclaration, dec.PropertyName.Text);
            //    context.AddError(new SimpleErrorTag(dec.PropertyName, message));
            //    return ItemCheckResult.CancelCurrentItem;
            //}
            if (schema.GetProperty(dec.PropertyName.Text) == null)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.ValidationVendorDeclarations, dec.PropertyName.Text);
                context.AddError(new SimpleErrorTag(dec.PropertyName, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                return ItemCheckResult.CancelCurrentItem;
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

            if (normalized.Length > 0 && normalized[0] == '-' && schema.GetPseudo(item.Text) == null)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.ValidationVendorPseudo, item.Text);
                context.AddError(new SimpleErrorTag(item, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                return ItemCheckResult.CancelCurrentItem;
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

            if (schema.GetAtDirective("@" + dir.Keyword.Text) == null)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.ValidationVendorDirective, dir.Keyword.Text);
                context.AddError(new SimpleErrorTag(dir.Keyword, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                return ItemCheckResult.CancelCurrentItem;
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(AtDirective) }; }
        }
    }
}