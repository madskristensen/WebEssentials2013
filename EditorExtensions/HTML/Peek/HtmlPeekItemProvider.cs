using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
 
namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IPeekableItemSourceProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Name("HTML Peekable Item Provider")]
    [SupportsStandaloneFiles(false)]
    class HtmlPeekItemProvider : IPeekableItemSourceProvider
    {
        private readonly IPeekResultFactory _peekResultFactory;

        [ImportingConstructor]
        public HtmlPeekItemProvider(IPeekResultFactory peekResultFactory)
        {
            _peekResultFactory = peekResultFactory;
        }

        public IPeekableItemSource TryCreatePeekableItemSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new HtmlPeekItemSource(textBuffer, _peekResultFactory));
        }
    }
}
