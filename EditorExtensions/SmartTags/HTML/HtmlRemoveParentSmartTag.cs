using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.SmartTags;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.SmartTags
{
    [Export(typeof(IHtmlSmartTagProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Order(Before = "Default")]
    [Name("HtmlRemoveParentSmartTag")]
    internal class HtmlRemoveParentSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if (element.Children.Count > 0)
            {
                return new HtmlRemoveParentSmartTag(textView, textBuffer, element);
            }

            return null;
        }
    }

    internal class HtmlRemoveParentSmartTag : HtmlSmartTag
    {
        public HtmlRemoveParentSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.StartTag)
        { }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            yield return new RemoveParentSmartTagAction(this);
        }

        class RemoveParentSmartTagAction : HtmlSmartTagAction
        {
            public RemoveParentSmartTagAction(HtmlSmartTag htmlSmartTag) :
                base(htmlSmartTag, "Remove and keep children")
            { }

            public override void Invoke()
            {
                var element = HtmlSmartTag.Element;
                var textBuffer = HtmlSmartTag.TextBuffer;
                var view = HtmlSmartTag.TextView;

                var content = textBuffer.CurrentSnapshot.GetText(element.InnerRange.Start, element.InnerRange.Length).Trim();
                int start = element.Start;
                int length = content.Length;

                using (EditorExtensionsPackage.UndoContext((this.DisplayText)))
                {
                    textBuffer.Replace(new Span(element.Start, element.OuterRange.Length), content);

                    SnapshotSpan span = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, start, length);

                    view.Selection.Select(span, false);
                    EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
                    view.Caret.MoveTo(new SnapshotPoint(view.TextBuffer.CurrentSnapshot, start));
                    view.Selection.Clear();
                }
            }
        }
    }
}