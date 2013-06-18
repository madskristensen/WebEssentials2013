using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;

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

            if (function.FunctionName.Text.StartsWith("rgb"))
            {
                ValidateRgb(context, function);
            }
            //else if (function.FunctionName.Text.StartsWith("hsl"))
            //{
            //    ValidateHsl(context, function);
            //}

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
            for (int i = 0; i < function.Arguments.Count; i++)
            {
                var argument = function.Arguments[i];
                string text = argument.Text.Trim(',','%');

                if (i < 3)
                {
                    int value;
                    if (int.TryParse(text, out value) && (value < 0 || value > 100))
                        context.AddError(new SimpleErrorTag(argument, "Validation: Values must be between 0 and 100%", CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
                }
                else
                {
                    ValidateAlphaValue(context, argument, text);
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