using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Html
{
    class ClassDefinitionPeekItem : IPeekableItem
    {
        internal readonly IPeekResultFactory _peekResultFactory;
        internal string _className;
        internal readonly ITextBuffer _textbuffer;

        public ClassDefinitionPeekItem(string className, IPeekResultFactory peekResultFactory, ITextBuffer textbuffer)
        {
            _className = className;
            _peekResultFactory = peekResultFactory;
            _textbuffer = textbuffer;
        }

        public string DisplayName
        {
            // This is unused, and was supposed to have been removed from IPeekableItem.
            get { return null; }
        }

        public IEnumerable<IPeekRelationship> Relationships
        {
            get { yield return PredefinedPeekRelationships.Definitions; }
        }

        public IPeekResultSource GetOrCreateResultSource(string relationshipName)
        {
            return new ClassResultSource(this);
        }
    }
}