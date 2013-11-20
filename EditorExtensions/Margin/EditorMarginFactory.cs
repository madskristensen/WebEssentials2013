using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

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
    internal sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            string source = textViewHost.TextView.TextBuffer.CurrentSnapshot.GetText();
            ITextDocument document;

            if (textViewHost.TextView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document))
            {
                switch (textViewHost.TextView.TextBuffer.ContentType.DisplayName.ToLowerInvariant())
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