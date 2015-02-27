﻿using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.CoffeeScript;
using MadsKristensen.EditorExtensions.Handlebars;
using MadsKristensen.EditorExtensions.IcedCoffeeScript;
using MadsKristensen.EditorExtensions.LiveScript;
using MadsKristensen.EditorExtensions.SweetJs;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Compilers
{
    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("Markdown")]
    class MarkdownCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return doc.TextBuffer.Properties.GetOrCreateSingletonProperty<EditorCompilerInvoker>(
                       () => new EditorCompilerInvoker(doc, new MarkdownCompilerRunner(doc.TextBuffer.ContentType, doc))
                   );
        }
    }

    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("Htmlx")]
    internal class HtmlxCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return doc.TextBuffer.Properties.GetOrCreateSingletonProperty<EditorCompilerInvoker>(
                       () => new EditorCompilerInvoker(doc, new RazorZenCompilerRunner(doc.TextBuffer.ContentType, doc))
                   );
        }
    }

    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("Razor")]
    internal class RazorCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return doc.TextBuffer.Properties.GetOrCreateSingletonProperty<EditorCompilerInvoker>(
                       () => new EditorCompilerInvoker(doc, new RazorZenCompilerRunner(doc.TextBuffer.ContentType, doc))
                   );
        }
    }

    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType(CssContentTypeDefinition.CssContentType)]
    [ContentType(LessContentTypeDefinition.LessContentType)]
    [ContentType(ScssContentTypeDefinition.ScssContentType)]
    [ContentType(CoffeeContentTypeDefinition.CoffeeContentType)]
    [ContentType(CsonContentTypeDefinition.CsonContentType)]
    [ContentType(IcedCoffeeScriptContentTypeDefinition.IcedCoffeeScriptContentType)]
    [ContentType(LiveScriptContentTypeDefinition.LiveScriptContentType)]
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    [ContentType(HandlebarsContentTypeDefinition.HandlebarsContentType)]
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
