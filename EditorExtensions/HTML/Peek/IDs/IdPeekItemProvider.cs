using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IPeekableItemSourceProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Name("HTML ID Peekable Item Provider")]
    [SupportsStandaloneFiles(false)]
    class IdPeekItemProvider : IPeekableItemSourceProvider
    {
#pragma warning disable 649 // "field never assigned to" -- field is set by MEF.
        [Import]
        private IPeekResultFactory _peekResultFactory;
#pragma warning restore 649

        public IPeekableItemSource TryCreatePeekableItemSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new IdPeekItemSource(textBuffer, _peekResultFactory));
        }
    }
}