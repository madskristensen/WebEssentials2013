using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Html
{
    internal sealed class IdPeekItemSource : IPeekableItemSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly IPeekResultFactory _peekResultFactory;

        public IdPeekItemSource(ITextBuffer textBuffer, IPeekResultFactory peekResultFactory)
        {
            _textBuffer = textBuffer;
            _peekResultFactory = peekResultFactory;
        }

        public void AugmentPeekSession(IPeekSession session, IList<IPeekableItem> peekableItems)
        {
            var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
                return;

            string id = HtmlHelpers.GetSinglePropertyValue(_textBuffer, triggerPoint.Value.Position, "id");
            if (string.IsNullOrEmpty(id))
                return;


            peekableItems.Add(new IdDefinitionPeekItem(id, _peekResultFactory, _textBuffer));
        }

        public void Dispose()
        {
        }
    }
}
