using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    public static class MarkdownClassificationTypes
    {
        public const string MarkdownBold = "md_bold";
        public const string MarkdownItalic = "md_italic";
        public const string MarkdownHeader = "md_header";
        public const string MarkdownCode = "md_code";
        public const string MarkdownQuote = "md_quote";

        [Export, Name(MarkdownClassificationTypes.MarkdownBold)]
        public static ClassificationTypeDefinition MarkdownClassificationBold { get; set; }

        [Export, Name(MarkdownClassificationTypes.MarkdownItalic)]
        public static ClassificationTypeDefinition MarkdownClassificationItalic { get; set; }

        [Export, Name(MarkdownClassificationTypes.MarkdownHeader)]
        public static ClassificationTypeDefinition MarkdownClassificationHeader { get; set; }

        [Export, Name(MarkdownClassificationTypes.MarkdownCode)]
        public static ClassificationTypeDefinition MarkdownClassificationCode { get; set; }

        [Export, Name(MarkdownClassificationTypes.MarkdownQuote)]
        public static ClassificationTypeDefinition MarkdownClassificationQuote { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownBold)]
    [Name(MarkdownClassificationTypes.MarkdownBold)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class MarkdownBoldFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownBoldFormatDefinition()
        {
            IsBold = true;
            DisplayName = "Markdown Bold";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownItalic)]
    [Name(MarkdownClassificationTypes.MarkdownItalic)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class MarkdownItalicFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownItalicFormatDefinition()
        {
            IsItalic = true;
            DisplayName = "Markdown Italic";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownHeader)]
    [Name(MarkdownClassificationTypes.MarkdownHeader)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class MarkdownHeaderFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownHeaderFormatDefinition()
        {
            IsBold = true;
            TextDecorations = new TextDecorationCollection();
            TextDecorations.Add(new TextDecoration());
            DisplayName = "Markdown Headers";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownCode)]
    [Name(MarkdownClassificationTypes.MarkdownCode)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class MarkdownCodeFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownCodeFormatDefinition()
        {
            BackgroundColor = Color.FromRgb(255, 255, 192);
            BackgroundOpacity = .2;
            DisplayName = "Markdown Code";
        }
    }
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownQuote)]
    [Name(MarkdownClassificationTypes.MarkdownQuote)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class MarkdownQuoteFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownQuoteFormatDefinition()
        {
            // I wish I could make the background apply block-level (to highlight the entire line)
            BackgroundColor = Color.FromRgb(200, 200, 200);
            BackgroundOpacity = .2;
            DisplayName = "Markdown Quote";
        }
    }
}
