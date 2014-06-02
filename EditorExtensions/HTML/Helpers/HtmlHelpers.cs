using System;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Html
{
    public static class HtmlHelpers
    {
        /// <summary>
        /// Finds a single property value at a given position in the TextBuffer.
        /// </summary>
        /// <param name="buffer">Any text buffer</param>
        /// <param name="position">The position in the buffer</param>
        /// <param name="attributeNames">One or more HTMLattribute names, such as 'class', 'id', 'src' etc.</param>
        /// <returns>A single value matching the position in the text buffer</returns>
        public static string GetSinglePropertyValue(ITextBuffer buffer, int position, params string[] attributeNames)
        {
            var document = HtmlEditorDocument.FromTextBuffer(buffer);
            if (document == null)
                return null;

            ElementNode element = null;
            AttributeNode attr = null;

            document.HtmlEditorTree.GetPositionElement(position, out element, out attr);

            if (attr == null || !attributeNames.Contains(attr.Name, StringComparer.InvariantCultureIgnoreCase))
                return null;

            int beginning = position - attr.ValueRangeUnquoted.Start;
            int start = attr.Value.LastIndexOf(' ', beginning) + 1;
            int length = attr.Value.IndexOf(' ', start) - start;

            if (length < 0)
                length = attr.ValueRangeUnquoted.Length - start;

            return attr.Value.Substring(start, length);
        }
    }
}