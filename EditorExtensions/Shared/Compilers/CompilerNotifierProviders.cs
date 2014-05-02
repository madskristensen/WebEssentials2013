using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.SweetJs;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Compilers
{
    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("Markdown")]
    class MarkdownCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return doc.TextBuffer.Properties.GetOrCreateSingletonProperty<EditorCompilerInvoker>(
                       () => new EditorCompilerInvoker(doc, new MarkdownCompilerRunner(doc.TextBuffer.ContentType))
                   );
        }
    }

    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("LESS")]
    [ContentType("SCSS")]
    [ContentType("CoffeeScript")]
    [ContentType("IcedCoffeeScript")]
    [ContentType(LiveScript.LiveScriptContentTypeDefinition.LiveScriptContentType)]
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    class NodeCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return doc.TextBuffer.Properties.GetOrCreateSingletonProperty<ErrorReportingCompilerInvoker>(
                       () => new ErrorReportingCompilerInvoker(doc, new NodeCompilerRunner(doc.TextBuffer.ContentType))
                   );
        }
    }
}
