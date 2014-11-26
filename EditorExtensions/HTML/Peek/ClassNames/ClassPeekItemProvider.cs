using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IPeekableItemSourceProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Name("HTML Class Peekable Item Provider")]
    [SupportsStandaloneFiles(false)]
    class ClassPeekItemProvider : IPeekableItemSourceProvider
    {
        [Import]
        public IPeekResultFactory _peekResultFactory;

        public IPeekableItemSource TryCreatePeekableItemSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new ClassPeekItemSource(textBuffer, _peekResultFactory));
        }
    }
}