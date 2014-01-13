using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("MarginFactory")]
    [Order(After = PredefinedMarginNames.RightControl)]
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType("LESS")]
    [ContentType("SASS")]
    [ContentType("CoffeeScript")]
    [ContentType(IcedCoffeeScriptContentTypeDefinition.IcedCoffeeScriptContentType)]
    [ContentType("TypeScript")]
    [ContentType("Markdown")]
    [ContentType(SvgContentTypeDefinition.SvgContentType)]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)]
    public sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        static readonly Dictionary<string, Func<string, ITextDocument, IWpfTextViewMargin>> marginFactories = new Dictionary<string, Func<string, ITextDocument, IWpfTextViewMargin>>(StringComparer.OrdinalIgnoreCase)
        {
            { "LESS",              (source, document) => new LessMargin("CSS", source, document) },
            { "SASS",              (source, document) => new SassMargin("CSS", source, document) },
            { "CoffeeScript",      (source, document) => new CoffeeScriptMargin("JavaScript", source, document) },
            { "IcedCoffeeScript",  (source, document) => new IcedCoffeeScriptMargin("JavaScript", source, document) },
            { "TypeScript",        (source, document) => new TypeScriptMargin("JavaScript", source, document) },
            { "Markdown",          (source, document) => new MarkdownMargin("text", source, document) },
            { "Svg",               (source, document) => new SvgMargin("svg", source, document) }
        };

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            Func<string, ITextDocument, IWpfTextViewMargin> creator;
            if (!marginFactories.TryGetValue(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer.ContentType.TypeName, out creator))
                return null;

            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer, out document))
                return null;

            string source = wpfTextViewHost.TextView.TextBuffer.CurrentSnapshot.GetText();

            return creator(source, document);
        }
    }
}