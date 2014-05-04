using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.IcedCoffeeScript;
using MadsKristensen.EditorExtensions.LiveScript;
using MadsKristensen.EditorExtensions.Markdown;
using MadsKristensen.EditorExtensions.Svg;
using MadsKristensen.EditorExtensions.SweetJs;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Margin
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("MarginFactory")]
    [Order(After = PredefinedMarginNames.RightControl)]
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType(LessContentTypeDefinition.LessContentType)]
    [ContentType(ScssContentTypeDefinition.ScssContentType)]
    [ContentType(CoffeeContentTypeDefinition.CoffeeContentType)]
    [ContentType(IcedCoffeeScriptContentTypeDefinition.IcedCoffeeScriptContentType)]
    [ContentType(LiveScriptContentTypeDefinition.LiveScriptContentType)]
    [ContentType("TypeScript")]
    [ContentType("Markdown")]
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    [ContentType(SvgContentTypeDefinition.SvgContentType)]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)]
    public sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        static readonly Dictionary<string, Func<ITextDocument, IWpfTextView, IWpfTextViewMargin>> marginFactories = new Dictionary<string, Func<ITextDocument, IWpfTextView, IWpfTextViewMargin>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CoffeeScript",      (document, sourceView) => new TextViewMargin("JavaScript", document, sourceView) },
            { "IcedCoffeeScript",  (document, sourceView) => new TextViewMargin("JavaScript", document, sourceView) },
            { "LiveScript",        (document, sourceView) => new TextViewMargin("JavaScript", document, sourceView) },
            { "LESS",              (document, sourceView) => new CssTextViewMargin("CSS", document, sourceView) },
            { "Markdown",          (document, sourceView) => new MarkdownMargin(document) },
            { "SCSS",              (document, sourceView) => new CssTextViewMargin("CSS", document, sourceView) },
            { "Svg",               (document, sourceView) => new SvgMargin(document) },
            { "SweetJs",           (document, sourceView) => new TextViewMargin("JavaScript", document, sourceView) },
            { "TypeScript",        (document, sourceView) => document.FilePath.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase) 
                                                             ? null : new TextViewMargin("JavaScript", document, sourceView) }
        };

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            Func<ITextDocument, IWpfTextView, IWpfTextViewMargin> creator;
            if (!marginFactories.TryGetValue(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer.ContentType.TypeName, out creator))
                return null;

            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer, out document))
                return null;

            return creator(document, wpfTextViewHost.TextView);
        }
    }
}