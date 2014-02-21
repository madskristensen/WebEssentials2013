using Microsoft.Html.Core;
using Microsoft.Html.Editor.SmartTags;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;

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
            AttributeNode attr = GetAttribute(element);
            string url = NormalizeUrl(attr.Value);

            if (attr != null && !string.IsNullOrEmpty(url))
            {
                return new RemoteDownloaderSmartTag(textView, textBuffer, element, attr);
            }

            return null;
        }

        private static AttributeNode GetAttribute(ElementNode element)
        {
            if (element.Name.Equals("link"))
                return element.GetAttribute("href");

            return element.GetAttribute("src");

        }

        public static string NormalizeUrl(string value)
        {
            if (value.StartsWith("//", StringComparison.Ordinal))
                value = "http:" + value;

            Uri url;
            if (Uri.TryCreate(value, UriKind.Absolute, out url))
                return url.ToString();

            return null;
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
                ElementNode element = this.HtmlSmartTag.Element;
                string url = RemoteDownloaderSmartTagProvider.NormalizeUrl(_attribute.Value);
                string cleanUrl = CleanUrl(url);
                string extension = Path.GetExtension(cleanUrl).TrimStart('.');
                string name = Path.GetFileNameWithoutExtension(cleanUrl);
                string fileName = FileHelpers.ShowDialog(extension, name);

                if (!string.IsNullOrEmpty(fileName))
                {
                    DownloadFile(fileName);
                    ReplaceUrlValue(fileName, textBuffer, _attribute);
                }
            }

            private string CleanUrl(string url)
            {
                int index = url.IndexOf('?');
                if (index > -1)
                {
                    return url.Substring(0, index);
                }

                return url;
            }

            private void DownloadFile(string fileName)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(_attribute.Value, fileName);
                    }

                    ProjectHelpers.AddFileToActiveProject(fileName);
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
            }

            private static void ReplaceUrlValue(string fileName, ITextBuffer buffer, AttributeNode src)
            {
                string relative = FileHelpers.RelativePath(EditorExtensionsPackage.DTE.ActiveDocument.FullName, fileName);
                Span span = new Span(src.ValueRangeUnquoted.Start, src.ValueRangeUnquoted.Length);
                buffer.Replace(span, relative.ToLowerInvariant());
            }
        }
    }
}