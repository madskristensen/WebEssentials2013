using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.WebVtt
{
    public static class WebVttClassificationTypes
    {
        public const string Markup = "webvtt_markup";
        public const string Name = "webvtt_name";
        public const string Statement = "webvtt_statement";
        public const string Time = "webvtt_Time";
        public const string Comment = "webvtt_Comment";

        [Export, Name(WebVttClassificationTypes.Markup)]
        public static ClassificationTypeDefinition WebVttClassificationMarkup { get; set; }

        [Export, Name(WebVttClassificationTypes.Name)]
        public static ClassificationTypeDefinition WebVttClassificationName { get; set; }

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
            ForegroundColor = System.Windows.Media.Colors.SteelBlue;
            IsBold = true;
            DisplayName = "WebVTT Markup";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = WebVttClassificationTypes.Name)]
    [Name(WebVttClassificationTypes.Name)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class WebVttNameFormatDefinition : ClassificationFormatDefinition
    {
        public WebVttNameFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Gray;
            IsBold = true;
            DisplayName = "WebVTT Name";
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
            ForegroundColor = System.Windows.Media.Colors.DarkOrange;
            DisplayName = "WebVTT Time";
        }
    }
}
