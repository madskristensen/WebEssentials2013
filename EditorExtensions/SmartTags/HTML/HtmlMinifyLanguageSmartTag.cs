using Microsoft.Html.Core;
using Microsoft.Html.Editor.SmartTags;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Core;
using Microsoft.Web.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.SmartTags
{
    [Export(typeof(IHtmlSmartTagProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Order(Before = "Default")]
    [Name("HtmlMinifySmartTagProvider")]
    internal class HtmlMinifySmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if (element.IsStyleBlock() || element.IsJavaScriptBlock())
            {
                return new HtmlMinifySmartTag(textView, textBuffer, element);
            }

            return null;
        }
    }

    internal class HtmlMinifySmartTag : HtmlSmartTag
    {
        public HtmlMinifySmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
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
                base(htmlSmartTag, "Minify")
            {
            }

            public override void Invoke()
            {
                var element = this.HtmlSmartTag.Element;
                var textBuffer = this.HtmlSmartTag.TextBuffer;

                ITextRange range = element.InnerRange;
                string text = textBuffer.CurrentSnapshot.GetText(range.Start, range.Length);
                string ext = element.IsScriptBlock() ? ".js" : ".css";
                string result = MinifyFileMenu.MinifyString(ext, text);

                EditorExtensionsPackage.DTE.UndoContext.Open(this.DisplayText);

                using (var edit = textBuffer.CreateEdit())
                {
                    edit.Replace(range.Start, range.Length, result);
                    edit.Apply();
                }
                
                EditorExtensionsPackage.DTE.UndoContext.Close();
            }
        }
    }
}
