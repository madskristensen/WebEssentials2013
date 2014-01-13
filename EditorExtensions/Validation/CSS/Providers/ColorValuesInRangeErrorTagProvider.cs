using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("ColorValuesInRangeErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class ColorValuesInRangeErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            FunctionColor function = (FunctionColor)item;

            if (!function.IsValid || context == null)
                return ItemCheckResult.Continue;

            if (function.FunctionName.Text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                ValidateRgb(context, function);
            }
            else if (function.FunctionName.Text.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
            {
                ValidateHsl(context, function);
            }

            return ItemCheckResult.Continue;
        }

        private static void ValidateRgb(ICssCheckerContext context, FunctionColor function)
        {
            for (int i = 0; i < function.Arguments.Count; i++)
            {
                var argument = function.Arguments[i];
                string text = argument.Text.Trim(',');

                if (i < 3)
                {
                    int value;
                    if (int.TryParse(text, out value) && (value < 0 || value > 255))
                        context.AddError(new SimpleErrorTag(argument, Resources.ValidationColorValuesInRange, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                }
                else
                {
                    ValidateAlphaValue(context, argument, text);
                }
            }
        }

        private static void ValidateHsl(ICssCheckerContext context, FunctionColor function)
        {
            ParseItem argument;
            string text = "";
            string[] pair;
            int argumentCount = function.Arguments.Count;
            int value;

            if (function.FunctionName.Text.StartsWith("hsla", StringComparison.OrdinalIgnoreCase) && argumentCount < 4)
            {
                context.AddError(new SimpleErrorTag(function.Arguments[2], "Validation: HSLA expects alpha value between 0 and 1 as fourth parameter", CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
            }

            for (int i = 0; i < argumentCount; i++)
            {
                argument = function.Arguments[i];
                text = argument.Text.Trim(',');

                if (i < 3)
                {
                    pair = text.Split('%');

                    if (int.TryParse(pair[0], out value))
                    {
                        if (value < 0 || value > 100 && i > 0)
                        {
                            context.AddError(new SimpleErrorTag(argument, "Validation: Values must be between 0 and 100%", CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                        }

                        if (i > 0 && value > 0 && pair[1] == "%")
                        {
                            context.AddError(new SimpleErrorTag(argument, "Validation: Parameter missing the % unit", CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                        }
                    }
                }
                else if (function.FunctionName.Text.StartsWith("hsla", StringComparison.OrdinalIgnoreCase))
                {
                    ValidateAlphaValue(context, argument, text);
                }
                else
                {
                    context.AddError(new SimpleErrorTag(argument, "Validation: HSL cannot have fourth parameter. Are you confusing HSL with HSLA?", CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                }
            }
        }

        private static void ValidateAlphaValue(ICssCheckerContext context, ParseItem argument, string text)
        {
            double value;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && (value < 0 || value > 1))
                context.AddError(new SimpleErrorTag(argument, "Validation: The opacity value must be between 0 and 1", CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(FunctionColor) }; }
        }
    }
}