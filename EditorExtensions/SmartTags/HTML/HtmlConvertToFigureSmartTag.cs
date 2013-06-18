using Microsoft.Html.Core;
using Microsoft.Html.Editor.SmartTags;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.SmartTags
{
    [Export(typeof(IHtmlSmartTagProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Order(Before = "Default")]
    [Name("HtmlConvertImgToFigure")]
    internal class HtmlConvertImgToFigureSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if (element.Name == "img")
            {
                return new HtmlConvertImgToFigureSmartTag(textView, textBuffer, element);
            }

            return null;
        }
    }

    internal class HtmlConvertImgToFigureSmartTag : HtmlSmartTag
    {
        public HtmlConvertImgToFigureSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.ElementName)
        {
        }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            return new ISmartTagAction[] { new FormatSelectionSmartTagAction(this) };
        }

        class FormatSelectionSmartTagAction : HtmlSmartTagAction
        {
            public FormatSelectionSmartTagAction(HtmlSmartTag htmlSmartTag) :
                base(htmlSmartTag, "Convert to <figure>")
            {
            }

            private string _figureHtml = "<figure>\n{0}\n<figcaption>Description</figcaption>\n</figure>";

            public override void Invoke()
            {
                var element = this.HtmlSmartTag.Element;
                var textBuffer = this.HtmlSmartTag.TextBuffer;
                
                int start = this.HtmlSmartTag.Element.Start;
                int length = this.HtmlSmartTag.Element.Length;

                string img = textBuffer.CurrentSnapshot.GetText(start, length);
                string figure = string.Format(_figureHtml, img);

                EditorExtensionsPackage.DTE.UndoContext.Open(this.DisplayText);

                using (var edit = textBuffer.CreateEdit())
                {
                    edit.Replace(start, length, figure);
                    edit.Apply();
                }

                this.HtmlSmartTag.TextView.Caret.MoveToPreviousCaretPosition();
                EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
                this.HtmlSmartTag.TextView.Caret.MoveToNextCaretPosition();
                
                EditorExtensionsPackage.DTE.UndoContext.Close();
            }
        }
    }
}
