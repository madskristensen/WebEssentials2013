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
using System.Text;

namespace MadsKristensen.EditorExtensions.SmartTags
{
    [Export(typeof(IHtmlSmartTagProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Order(Before = "Default")]
    [Name("HtmlUnMinifySmartTagProvider")]
    internal class HtmlUnMinifySmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if (element.Children.Count > 1)
            {
                return new HtmlUnMinifySmartTag(textView, textBuffer, element);
            }

            return null;
        }
    }

    internal class HtmlUnMinifySmartTag : HtmlSmartTag
    {
        public HtmlUnMinifySmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.ElementName)
        { }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            return new ISmartTagAction[] { new FormatSelectionSmartTagAction(this) };
        }

        class FormatSelectionSmartTagAction : HtmlSmartTagAction
        {
            private static List<string> _blockElements = new List<string>(){
                "p",
                "div",
                "section",
                "article",
                "header",
                "footer",
                "aside",
                "table",
                "tr",
                "th",
                "td",
                "h1",
                "h2",
                "h3",
                "h4",
                "h5",
                "h6",
                "nav",
                "ul",
                "li",                
            };

            public FormatSelectionSmartTagAction(HtmlSmartTag htmlSmartTag) :
                base(htmlSmartTag, "Un-minify the element")
            { }

            public override void Invoke()
            {
                var element = this.HtmlSmartTag.Element;
                var textBuffer = this.HtmlSmartTag.TextBuffer;

                EditorExtensionsPackage.DTE.UndoContext.Open(this.DisplayText);

                using (var edit = textBuffer.CreateEdit())
                {
                    Unminify(element, edit);
                    edit.Apply();
                }

                EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");

                EditorExtensionsPackage.DTE.UndoContext.Close();
            }

            private void Unminify(ElementNode element, ITextEdit edit)
            {
                for (int i = element.Children.Count - 1; i > -1; i--)
                {
                    var child = element.Children[i];

                    Unminify(child, edit);
                }

                if (_blockElements.Contains(element.Name))
                {
                    int start = element.StartTag.Start;
                    edit.Insert(start, Environment.NewLine);
                }
                else if (_blockElements.Contains(element.Parent.Name) && element.Parent.Children.Count > 1)
                {
                    int start = element.StartTag.Start;
                    edit.Insert(start, Environment.NewLine);
                }
            }
        }
    }
}
