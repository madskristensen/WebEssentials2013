using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.SmartTags;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    // TODO: Require modificaiton
    [Export(typeof(IHtmlSmartTagProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Order(Before = "Default")]
    [Name("DropImagesSmartTagProvider")]
    internal class DropImagesSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, Microsoft.Html.Core.ElementNode element, Microsoft.Html.Core.AttributeNode attribute, int caretPosition, Microsoft.Html.Core.HtmlPositionType positionType)
        {
            return new DropImagesSmartTag(textView, textBuffer, element);
        }
    }

    internal class DropImagesSmartTag : HtmlSmartTag
    {
        public DropImagesSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.StartTag)
        {
        }

        protected override IEnumerable<Microsoft.VisualStudio.Language.Intellisense.ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            yield return new DropImagesSmartTagAction(this);
        }

        class DropImagesSmartTagAction : HtmlSmartTagAction
        {
            public DropImagesSmartTagAction(HtmlSmartTag htmlSmartTag) :
                base(htmlSmartTag, "DropImages")
            {
            }

            public override void Invoke()
            {
                // This collection needs to be coming from editor; the list of file paths dropped
                IEnumerable<string> imagesCollection = null;

                imagesCollection = imagesCollection.Where(image => new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico" }
                                                           .Contains(Path.GetExtension(image)));

                string markup = new ImageDropFormattingDialog(imagesCollection).ShowAsDialog();

                // Here we need to write back to Editor
                throw new NotImplementedException();
            }
        }
    }
}