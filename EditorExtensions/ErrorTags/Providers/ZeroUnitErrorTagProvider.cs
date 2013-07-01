using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("ZeroUnitErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class ZeroUnitErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.ValidateZeroUnit))
                return ItemCheckResult.Continue;

            NumericalValue number = (NumericalValue)item;
            UnitValue unit = number as UnitValue;

            if (unit == null || context == null)
                return ItemCheckResult.Continue;

            // The 2nd and 3rd arguments to hsl() require units even when zero
            var function = unit.Parent.Parent as FunctionColor;
            if (function != null && function.FunctionName.Text.StartsWith("hsl", StringComparison.OrdinalIgnoreCase)) {
                var arg = unit.Parent as FunctionArgument;
                if (arg != function.Arguments[0])
                    return ItemCheckResult.Continue;
            }

            if (number.Number.Text == "0" && unit.UnitType != UnitType.Unknown && unit.UnitType != UnitType.Time)
            {
                string message = string.Format(Resources.BestPracticeZeroUnit, unit.UnitToken.Text);
                context.AddError(new SimpleErrorTag(number, message));
            }

            return ItemCheckResult.Continue;
        }

        private static UnitType GetUnitType(ParseItem valueItem)
        {
            UnitValue unitValue = valueItem as UnitValue;

            return (unitValue != null) ? unitValue.UnitType : UnitType.Unknown;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(NumericalValue) }; }
        }
    }
}