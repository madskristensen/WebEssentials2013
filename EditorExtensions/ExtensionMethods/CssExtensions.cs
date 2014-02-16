using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Schemas;

namespace MadsKristensen.EditorExtensions
{
    public static class CssExtensions
    {
        ///<summary>Gets the selector portion of the text of a Selector object, excluding any trailing comma.</summary>
        public static string SelectorText(this Selector selector)
        {
            if (selector.Comma == null) return selector.Text;
            return selector.Text.Substring(0, selector.Comma.Start - selector.Start).Trim();
        }

        public static bool IsPseudoElement(this ParseItem item)
        {
            if (item.Text.StartsWith("::", StringComparison.Ordinal))
                return true;

            var schema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
            return schema.GetPseudo(":" + item.Text) != null;
        }

        public static bool IsDataUri(this UrlItem item)
        {
            if (item.UrlString == null || string.IsNullOrEmpty(item.UrlString.Text))
                return false;

            return item.UrlString.Text.Contains(";base64,");
        }

        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags", Justification = "Match enum name")]
        public static CssErrorFlags ToCssErrorFlags(this WarningLocation location)
        {
            switch (location)
            {
                case WarningLocation.Warnings:
                    return CssErrorFlags.UnderlinePurple | CssErrorFlags.TaskListWarning;

                default:
                    return CssErrorFlags.UnderlinePurple | CssErrorFlags.TaskListMessage;
            }
        }
    }
}
