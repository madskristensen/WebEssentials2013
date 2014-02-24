using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Margin
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
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    [ContentType(SvgContentTypeDefinition.SvgContentType)]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)]
    public sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        static readonly Dictionary<string, Func<ITextDocument, IWpfTextViewMargin>> marginFactories = new Dictionary<string, Func<ITextDocument, IWpfTextViewMargin>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Svg",               (document) => new SvgMargin(document) },
            { "Markdown",          (document) => new MarkdownMargin(document) },
            { "LESS",              (document) => new TextViewMargin("CSS", document) },
            { "SASS",              (document) => new TextViewMargin("CSS", document) },
            { "CoffeeScript",      (document) => new TextViewMargin("JavaScript", document) },
            { "IcedCoffeeScript",  (document) => new TextViewMargin("JavaScript", document) },
            { "TypeScript",        (document) => document.FilePath.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase) 
                                                ? null : new TextViewMargin("JavaScript", document) },
            { "SweetJs",  (document) => new TextViewMargin("JavaScript", document) }
        };

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            Func<ITextDocument, IWpfTextViewMargin> creator;
            if (!marginFactories.TryGetValue(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer.ContentType.TypeName, out creator))
                return null;

            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer, out document))
                return null;

            return creator(document);
        }
    }
}