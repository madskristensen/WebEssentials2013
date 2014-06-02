using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Html
{
    class HtmlDefinitionPeekItem : IPeekableItem
    {
        internal readonly IPeekResultFactory _peekResultFactory;
        internal readonly string _className;
        internal readonly ITextBuffer _textbuffer;

        public HtmlDefinitionPeekItem(string className, IPeekResultFactory peekResultFactory, ITextBuffer textbuffer)
        {
            _peekResultFactory = peekResultFactory;
            _className = className;
            _textbuffer = textbuffer;
        }

        public string DisplayName
        {
            get
            {
                // This is unused, and was supposed to have been removed from IPeekableItem.
                return null;
            }
        }

        public IEnumerable<IPeekRelationship> Relationships
        {
            get
            {
                yield return PredefinedPeekRelationships.Definitions;
            }
        }

        public IPeekResultSource GetOrCreateResultSource(string relationshipName)
        {
            return new ResultSource(this);
        }
    }
}
