using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Html
{
    internal sealed class ClassPeekItemSource : IPeekableItemSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly IPeekResultFactory _peekResultFactory;

        public ClassPeekItemSource(ITextBuffer textBuffer, IPeekResultFactory peekResultFactory)
        {
            _textBuffer = textBuffer;
            _peekResultFactory = peekResultFactory;
        }

        public void AugmentPeekSession(IPeekSession session, IList<IPeekableItem> peekableItems)
        {
            var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
                return;

            string className = HtmlHelpers.GetSinglePropertyValue(_textBuffer, triggerPoint.Value.Position, "class");
            if (string.IsNullOrEmpty(className))
                return;


            peekableItems.Add(new ClassDefinitionPeekItem(className, _peekResultFactory, _textBuffer));
        }

        public void Dispose()
        { }
    }
}
