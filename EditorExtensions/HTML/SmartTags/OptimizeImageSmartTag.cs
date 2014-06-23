using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using MadsKristensen.EditorExtensions.Images;
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
    [Name("OptimizeImageSmartTagProvider")]
    internal class OptimizeImageSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if (IsEnabled(element))
            {
                return new OptimizeImageSmartTag(textView, textBuffer, element);
            }

            return null;
        }

        private static bool IsEnabled(ElementNode element)
        {
            if (element.Name != "img")
                return false;

            return element.HasAttribute("src");
        }
    }

    internal class OptimizeImageSmartTag : HtmlSmartTag
    {
        public OptimizeImageSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.StartTag)
        { }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            yield return new OptimizeImageSmartTagAction(this);
        }

        class OptimizeImageSmartTagAction : HtmlSmartTagAction
        {
            public OptimizeImageSmartTagAction(HtmlSmartTag htmlSmartTag) :
                base(htmlSmartTag, "Optimize image")
            { }

            public async override void Invoke()
            {
                ITextBuffer textBuffer = this.HtmlSmartTag.TextBuffer;
                ElementNode element = this.HtmlSmartTag.Element;
                AttributeNode src = element.GetAttribute("src", true);
                ImageCompressor compressor = new ImageCompressor();

                bool isDataUri = src.Value.StartsWith("data:image/", StringComparison.Ordinal);

                if (isDataUri)
                {
                    string dataUri = await compressor.CompressDataUriAsync(src.Value);

                    if (dataUri.Length < src.Value.Length)
                    {
                        using (WebEssentialsPackage.UndoContext("Optimize image"))
                        {
                            Span span = Span.FromBounds(src.ValueRangeUnquoted.Start, src.ValueRangeUnquoted.End);
                            textBuffer.Replace(span, dataUri);
                        }
                    }
                }
                else
                {
                    var fileName = ImageQuickInfo.GetFullUrl(src.Value, textBuffer);

                    if (string.IsNullOrEmpty(fileName) || !ImageCompressor.IsFileSupported(fileName) || !File.Exists(fileName))
                        return;

                    await compressor.CompressFilesAsync(fileName);
                }
            }
        }
    }
}