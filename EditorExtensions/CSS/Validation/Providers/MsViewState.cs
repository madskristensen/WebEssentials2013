using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("MsViewState")]
    [Order(After = "Default Declaration")]
    internal class MsViewStateErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            var media = item as MediaExpression;

            if (media == null || context == null || !IsWindowsWebApp())
                return ItemCheckResult.Continue;

            int index = media.Text.IndexOf("-ms-view-state", StringComparison.OrdinalIgnoreCase);

            if (index > -1)
            {
                var property = item.StyleSheet.ItemAfterPosition(media.Start + index);

                string message = "The -ms-view-state has been deprecated in Internet Explorer 11";
                context.AddError(new SimpleErrorTag(property, message, CssErrorFlags.TaskListWarning | CssErrorFlags.UnderlineRed));
            }

            return ItemCheckResult.Continue;
        }

        private static bool IsWindowsWebApp()
        {
            // TODO: Add logic to determine if the current project is WWA
            return true;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(MediaExpression) }; }
        }
    }
}