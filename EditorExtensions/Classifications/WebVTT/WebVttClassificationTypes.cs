using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public static class WebVttClassificationTypes
    {
        public const string Markup = "webvtt_markup";
        public const string Statement = "webvtt_statement";
        public const string Time = "webvtt_Time";

        [Export, Name(WebVttClassificationTypes.Markup)]
        public static ClassificationTypeDefinition WebVttClassificationMarkup { get; set; }

        [Export, Name(WebVttClassificationTypes.Statement)]
        public static ClassificationTypeDefinition WebVttClassificationStatement { get; set; }

        [Export, Name(WebVttClassificationTypes.Time)]
        public static ClassificationTypeDefinition WebVttClassificationTime { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = WebVttClassificationTypes.Markup)]
    [Name(WebVttClassificationTypes.Markup)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class WebVttMarkupFormatDefinition : ClassificationFormatDefinition
    {
        public WebVttMarkupFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Gray;
            DisplayName = "WebVTT Markup";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = WebVttClassificationTypes.Statement)]
    [Name(WebVttClassificationTypes.Statement)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class WebVttStatementFormatDefinition : ClassificationFormatDefinition
    {
        public WebVttStatementFormatDefinition()
        {
            IsItalic = true;
            IsBold = true;
            DisplayName = "WebVTT Statement";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = WebVttClassificationTypes.Time)]
    [Name(WebVttClassificationTypes.Time)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class WebVttTimeFormatDefinition : ClassificationFormatDefinition
    {
        public WebVttTimeFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Purple;
            DisplayName = "WebVTT Time";
        }
    }
}
