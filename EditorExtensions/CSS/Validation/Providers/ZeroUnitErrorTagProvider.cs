using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssItemChecker))]
    [Name("ZeroUnitErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class ZeroUnitErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (!WESettings.Instance.Css.ValidateZeroUnit)
                return ItemCheckResult.Continue;

            NumericalValue number = (NumericalValue)item;
            UnitValue unit = number as UnitValue;

            if (unit == null || context == null || item.FindType<Declaration>() == null)
                return ItemCheckResult.Continue;

            // The 2nd and 3rd arguments to hsl() require units even when zero
            var function = unit.Parent.Parent as FunctionColor;
            if (function != null && function.FunctionName.Text.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
            {
                var arg = unit.Parent as FunctionArgument;
                if (arg != function.Arguments[0])
                    return ItemCheckResult.Continue;
            }

            if (number.Number.Text == "0" && unit.UnitType != UnitType.Unknown && unit.UnitType != UnitType.Time)
            {
                string message = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.BestPracticeZeroUnit, unit.UnitToken.Text);
                context.AddError(new SimpleErrorTag(number, message));
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(NumericalValue) }; }
        }
    }
}