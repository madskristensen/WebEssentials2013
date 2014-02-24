using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Compilers
{
    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("TypeScript")]
    class TypeScriptCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return new TypeScriptCompilationNotifier(doc);
        }
    }
    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("Markdown")]
    class MarkdownCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return new EditorCompilerInvoker(doc, new MarkdownCompilerRunner(doc.TextBuffer.ContentType));
        }
    }
    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("LESS")]
    [ContentType("SASS")]
    [ContentType("CoffeeScript")]
    [ContentType("IcedCoffeeScript")]
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    class NodeCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return new ErrorReportingCompilerInvoker(doc, new NodeCompilerRunner(doc.TextBuffer.ContentType));
        }
    }
}
