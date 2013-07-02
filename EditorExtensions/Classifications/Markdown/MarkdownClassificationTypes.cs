﻿using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows;

namespace MadsKristensen.EditorExtensions
{
    class MarkdownClassificationTypes
    {
        public const string MarkdownBold = "md_bold";
        public const string MarkdownItalic = "md_italic";
        public const string MarkdownHeader = "md_header";
        public const string MarkdownCode = "md_code";
        public const string MarkdownQuote = "md_quote";

        [Export, Name(MarkdownClassificationTypes.MarkdownBold), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition MarkdownClassificationBold = null;

        [Export, Name(MarkdownClassificationTypes.MarkdownItalic), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition MarkdownClassificationItalic = null;

        [Export, Name(MarkdownClassificationTypes.MarkdownHeader), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition MarkdownClassificationHeader = null;

        [Export, Name(MarkdownClassificationTypes.MarkdownCode), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition MarkdownClassificationCode = null;

        [Export, Name(MarkdownClassificationTypes.MarkdownQuote), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition MarkdownClassificationQuote= null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownBold)]
    [Name(MarkdownClassificationTypes.MarkdownBold)]
    [Order(After = Priority.Default)]
    internal sealed class MarkdownBoldFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownBoldFormatDefinition()
        {
            IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownItalic)]
    [Name(MarkdownClassificationTypes.MarkdownItalic)]
    [Order(After = Priority.Default)]
    internal sealed class MarkdownItalicFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownItalicFormatDefinition()
        {
            IsItalic = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownHeader)]
    [Name(MarkdownClassificationTypes.MarkdownHeader)]
    [Order(After = Priority.Default)]
    internal sealed class MarkdownHeaderFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownHeaderFormatDefinition()
        {
            IsBold = true;
            TextDecorations = new TextDecorationCollection();
            TextDecorations.Add(new TextDecoration());
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownCode)]
    [Name(MarkdownClassificationTypes.MarkdownCode)]
    [Order(After = Priority.Default)]
    internal sealed class MarkdownCodeFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownCodeFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Green;
        }
    }
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownQuote)]
    [Name(MarkdownClassificationTypes.MarkdownQuote)]
    [Order(After = Priority.Default)]
    internal sealed class MarkdownQuoteFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownQuoteFormatDefinition()
        {
            // I wish I could make the background apply block-level (to highlight the entire line)
            BackgroundColor = System.Windows.Media.Color.FromRgb(200, 200, 200);
            BackgroundOpacity = .2;
        }
    }
}
