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
    [Name("HtmlRemoveParentSmartTag")]
    internal class HtmlRemoveParentSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if (element.InnerRange == null || element.GetText(element.InnerRange).Trim().Length == 0)
                return null;

            string displayText = element.Children.Count == 0 ? "Remove HTML tag" : "Remove and keep children";

            return new HtmlRemoveParentSmartTag(textView, textBuffer, element, displayText);
        }
    }

    internal class HtmlRemoveParentSmartTag : HtmlSmartTag
    {
        private string _displayText;
        public HtmlRemoveParentSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, string displayText)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.StartTag)
        {
            _displayText = displayText;
        }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            yield return new RemoveParentSmartTagAction(this, _displayText);
        }

        class RemoveParentSmartTagAction : HtmlSmartTagAction
        {
            public RemoveParentSmartTagAction(HtmlSmartTag htmlSmartTag, string displayText) :
                base(htmlSmartTag, displayText)
            { }

            public override void Invoke()
            {
                var element = HtmlSmartTag.Element;
                var textBuffer = HtmlSmartTag.TextBuffer;
                var view = HtmlSmartTag.TextView;

                var content = textBuffer.CurrentSnapshot.GetText(element.InnerRange.Start, element.InnerRange.Length).Trim();
                int start = element.Start;
                int length = content.Length;

                using (WebEssentialsPackage.UndoContext((this.DisplayText)))
                {
                    textBuffer.Replace(new Span(element.Start, element.OuterRange.Length), content);

                    SnapshotSpan span = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, start, length);

                    view.Selection.Select(span, false);
                    WebEssentialsPackage.ExecuteCommand("Edit.FormatSelection");
                    view.Caret.MoveTo(new SnapshotPoint(view.TextBuffer.CurrentSnapshot, start));
                    view.Selection.Clear();
                }
            }
        }
    }
}