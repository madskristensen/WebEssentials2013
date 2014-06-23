using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.SmartTags;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IHtmlSmartTagProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Order(Before = "Default")]
    [Name("AddClassSmartTagProvider")]
    internal class Base64DecodeSmartTagSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if (IsEnabled(element))
            {
                return new Base64DecodeSmartTagSmartTag(textView, textBuffer, element);
            }

            return null;
        }

        private static bool IsEnabled(ElementNode element)
        {
            if (element.Name != "img")
                return false;

            AttributeNode src = element.GetAttribute("src", true);

            if (src != null && src.Value.StartsWith("data:image/", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }

    internal class Base64DecodeSmartTagSmartTag : HtmlSmartTag
    {
        public Base64DecodeSmartTagSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.StartTag)
        { }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            yield return new Base64DecodeSmartTagTagAction(this);
        }

        class Base64DecodeSmartTagTagAction : HtmlSmartTagAction
        {
            public Base64DecodeSmartTagTagAction(HtmlSmartTag htmlSmartTag) :
                base(htmlSmartTag, "Save as image")
            { }

            public async override void Invoke()
            {
                ITextBuffer textBuffer = this.HtmlSmartTag.TextBuffer;
                ElementNode element = this.HtmlSmartTag.Element;
                AttributeNode src = element.GetAttribute("src", true);

                string mimeType = FileHelpers.GetMimeTypeFromBase64(src.Value);
                string extension = FileHelpers.GetExtension(mimeType) ?? "png";

                var fileName = FileHelpers.ShowDialog(extension);

                if (!string.IsNullOrEmpty(fileName) && await FileHelpers.SaveDataUriToFile(src.Value, fileName))
                {
                    using (WebEssentialsPackage.UndoContext((DisplayText)))
                        ReplaceUrlValue(fileName, textBuffer, src);
                }
            }

            private static void ReplaceUrlValue(string fileName, ITextBuffer buffer, AttributeNode src)
            {
                string relative = FileHelpers.RelativePath(buffer.GetFileName(), fileName);
                Span span = new Span(src.ValueRangeUnquoted.Start, src.ValueRangeUnquoted.Length);
                buffer.Replace(span, relative.ToLowerInvariant());
            }
        }
    }
}