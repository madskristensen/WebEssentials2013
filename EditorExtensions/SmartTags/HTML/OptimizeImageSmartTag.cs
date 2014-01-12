using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.SmartTags;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.SmartTags.HTML
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

            return element.GetAttribute("src", true) != null;
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
                    string mimeType = FileHelpers.GetMimeTypeFromBase64(src.Value);
                    string extension = FileHelpers.GetExtension(mimeType);

                    if (!ImageCompressor.IsFileSupported("file." + extension))
                        return;

                    string temp = Path.ChangeExtension(Path.GetTempFileName(), "." + extension);
                    bool isFileSaved = ReverseEmbedSmartTagAction.TrySaveFile(src.Value, temp);

                    if (isFileSaved)
                    {
                        await compressor.CompressFiles(temp);
                        string base64 = FileHelpers.ConvertToBase64(temp);
                        File.Delete(temp);

                        using (EditorExtensionsPackage.UndoContext("Optimize image"))
                        {
                            Span span = Span.FromBounds(src.ValueRangeUnquoted.Start, src.ValueRangeUnquoted.End);
                            textBuffer.Replace(span, base64);
                        }
                    }
                }
                else
                {
                    var fileName = ImageQuickInfo.GetFullUrl(src.Value, textBuffer);

                    if (string.IsNullOrEmpty(fileName) || !ImageCompressor.IsFileSupported(fileName) || !File.Exists(fileName))
                        return;

                    await compressor.CompressFiles(fileName);
                }
            }
        }
    }
}