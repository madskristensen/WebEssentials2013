using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(LessMargin.MarginName)]
    [Order(After = PredefinedMarginNames.RightControl)]
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType("LESS")]
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
            { "LESS",              (source, document) => new LessMargin("CSS", source, WESettings.GetBoolean(WESettings.Keys.ShowLessPreviewWindow), document) },
            { "CoffeeScript",      (source, document) => new CoffeeScriptMargin("JavaScript", source, WESettings.GetBoolean(WESettings.Keys.ShowCoffeeScriptPreviewWindow), document) },
            { "IcedCoffeeScript",  (source, document) => new IcedCoffeeScriptMargin("JavaScript", source, WESettings.GetBoolean(WESettings.Keys.ShowCoffeeScriptPreviewWindow), document) },
            { "TypeScript",        (source, document) => new TypeScriptMargin("JavaScript", source, WESettings.GetBoolean(WESettings.Keys.ShowTypeScriptPreviewWindow), document) },
            { "Markdown",          (source, document) => new MarkdownMargin("text", source, WESettings.GetBoolean(WESettings.Keys.MarkdownShowPreviewWindow), document) },
            { "Svg",               (source, document) => new SvgMargin("svg", source, WESettings.GetBoolean(WESettings.Keys.SvgShowPreviewWindow), document) }
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