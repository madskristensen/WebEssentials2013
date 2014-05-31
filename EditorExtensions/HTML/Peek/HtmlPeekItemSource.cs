using System.Collections.Generic;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Html
{
    internal sealed class HtmlPeekItemSource : IPeekableItemSource, IHtmlTreeVisitor
    {
        private readonly ITextBuffer textBuffer;
        private readonly IPeekResultFactory peekResultFactory;

        public HtmlPeekItemSource(ITextBuffer textBuffer, IPeekResultFactory peekResultFactory)
        {
            this.textBuffer = textBuffer;
            this.peekResultFactory = peekResultFactory;
        }

        public void AugmentPeekSession(IPeekSession session, IList<IPeekableItem> peekableItems)
        {
            var triggerPoint = session.GetTriggerPoint(textBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
            {
                return;
            }

            var document = HtmlEditorDocument.FromTextBuffer(textBuffer);
            if (document == null)
            {
                return;
            }

            string className;
            if (!TryGetClassName(document.HtmlEditorTree, triggerPoint.Value.Position, out className))
                return;

            peekableItems.Add(new HtmlDefinitionPeekItem(className, peekResultFactory, textBuffer));
        }

        private bool TryGetClassName(HtmlEditorTree tree, int position, out string className)
        {
            className = null;

            ElementNode element = null;
            AttributeNode attr = null;

            tree.GetPositionElement(position, out element, out attr);

            if (attr == null || attr.Name != "class")
                return false;

            int beginning = position - attr.ValueRangeUnquoted.Start;
            int start = attr.Value.LastIndexOf(' ', beginning) + 1;
            int length = attr.Value.IndexOf(' ', start) - start;

            if (length < 0)
                length = attr.ValueRangeUnquoted.Length - start;

            className = attr.Value.Substring(start, length);

            return true;
        }

        public void Dispose()
        {
        }

        public bool Visit(ElementNode element, object parameter)
        {
            var classAttribute = element.GetAttribute("class");
            if (classAttribute != null)
            {
                var list = (HashSet<AttributeNode>)parameter;
                list.Add(classAttribute);
            }

            return true;
        }
    }
}
