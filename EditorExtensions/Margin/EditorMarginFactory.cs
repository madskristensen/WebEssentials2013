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
    //[ContentType("TypeScript")]
    [ContentType("Markdown")]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)]
    public sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            string source = wpfTextViewHost.TextView.TextBuffer.CurrentSnapshot.GetText();
            ITextDocument document;

            if (TextDocumentFactoryService.TryGetTextDocument(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer, out document))
            {
                switch (wpfTextViewHost.TextView.TextBuffer.ContentType.DisplayName.ToLowerInvariant())
                {
                    case "less":
                        bool showLess = WESettings.GetBoolean(WESettings.Keys.ShowLessPreviewWindow);
                        return new LessMargin("CSS", source, showLess, document);

                    case "coffeescript":
                        bool showCoffee = WESettings.GetBoolean(WESettings.Keys.ShowCoffeeScriptPreviewWindow);
                        return new CoffeeScriptMargin("JavaScript", source, showCoffee, document);

                    case "markdown":
                        bool showMarkdown = WESettings.GetBoolean(WESettings.Keys.MarkdownShowPreviewWindow);
                        return new MarkdownMargin("text", source, showMarkdown, document);
                }
            }

            return null;
        }
    }
}