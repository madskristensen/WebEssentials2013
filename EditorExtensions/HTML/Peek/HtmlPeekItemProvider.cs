using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly IPeekResultFactory peekResultFactory;

        [ImportingConstructor]
        public HtmlPeekItemProvider(IPeekResultFactory peekResultFactory)
        {
            this.peekResultFactory = peekResultFactory;
        }

        public IPeekableItemSource TryCreatePeekableItemSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new HtmlPeekItemSource(textBuffer, peekResultFactory));
        }
    }
}
