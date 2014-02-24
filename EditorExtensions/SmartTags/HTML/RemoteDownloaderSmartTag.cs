using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Threading.Tasks;
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
    [Name("RemoteDownloaderSmartTagProvider")]
    internal class RemoteDownloaderSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            AttributeNode attr = element.GetAttribute("src") ?? element.GetAttribute("href");

            if (attr == null)
                return null;

            Uri url = NormalizeUrl(attr);

            if (url == null || (!attr.Value.StartsWith("//", StringComparison.Ordinal) && !attr.Value.Contains("://")))
                return null;

            return new RemoteDownloaderSmartTag(textView, textBuffer, element, attr);
        }

        public static Uri NormalizeUrl(AttributeNode attribute)
        {
            string value = attribute.Value;

            if (value.StartsWith("//", StringComparison.Ordinal))
                value = "http:" + value;

            Uri url;
            Uri.TryCreate(value, UriKind.Absolute, out url);

            return url;
        }
    }

    internal class RemoteDownloaderSmartTag : HtmlSmartTag
    {
        private AttributeNode _attribute;

        public RemoteDownloaderSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.StartTag)
        {
            _attribute = attribute;
        }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            yield return new RemoteDownloaderSmartTagAction(this, _attribute);
        }

        class RemoteDownloaderSmartTagAction : HtmlSmartTagAction
        {
            private AttributeNode _attribute;

            public RemoteDownloaderSmartTagAction(HtmlSmartTag htmlSmartTag, AttributeNode attribute) :
                base(htmlSmartTag, "Download remote file")
            {
                _attribute = attribute;
            }

            public override void Invoke()
            {
                ITextBuffer textBuffer = this.HtmlSmartTag.TextBuffer;
                Uri url = RemoteDownloaderSmartTagProvider.NormalizeUrl(_attribute);
                string cleanUrl = url.GetLeftPart(UriPartial.Path);
                string extension = Path.GetExtension(cleanUrl).TrimStart('.');
                string name = Path.GetFileNameWithoutExtension(cleanUrl);
                string fileName = FileHelpers.ShowDialog(extension, name + ".");

                if (!string.IsNullOrEmpty(fileName))
                {
                    DownloadFileAsync(url, fileName).DontWait("Download a file from a remote source to the local file system.");
                    ReplaceUrlValue(fileName, textBuffer, _attribute);
                }
            }

            private async Task DownloadFileAsync(Uri url, string fileName)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        await client.DownloadFileTaskAsync(url, fileName);
                    }

                    ProjectHelpers.AddFileToActiveProject(fileName);
                }
                catch (Exception ex)
                {
                    Logger.ShowMessage(ex.Message);
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