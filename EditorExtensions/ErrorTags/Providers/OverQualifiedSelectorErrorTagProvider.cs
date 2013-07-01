﻿using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("OverQualifiedSelectorErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class OverQualifiedSelectorErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            Selector sel = (Selector)item;

            if (!WESettings.GetBoolean(WESettings.Keys.ValidateOverQualifiedSelector) || !sel.IsValid || context == null)
                return ItemCheckResult.Continue;

            var idPart = sel.SimpleSelectors.Skip(1).FirstOrDefault(s => s.Text.StartsWith("#"));
            if (idPart != null)
            {
                string remove = sel.Text.Substring(0, idPart.Start - sel.Start).TrimEnd();  // Remove the whitespace before the final part
                string errorMessage = string.Format(CultureInfo.InvariantCulture, Resources.PerformanceDontOverQualifySelectors, idPart.Text, remove);

                SimpleErrorTag tag = new SimpleErrorTag(sel, errorMessage, remove.Length);

                context.AddError(tag);
            }

            return ItemCheckResult.Continue;
        }


        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(Selector) }; }
        }
    }
}
