using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public static class RobotsTxtClassificationTypes
    {
        public const string Keyword = "robotstxt_keyword";
        public const string Comment = "robotstxt_comment";

        [Export, Name(RobotsTxtClassificationTypes.Keyword)]
        public static ClassificationTypeDefinition RobotsTxtClassificationBold { get; set; }

        [Export, Name(RobotsTxtClassificationTypes.Comment)]
        public static ClassificationTypeDefinition RobotsTxtClassificationHeader { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = RobotsTxtClassificationTypes.Keyword)]
    [Name(RobotsTxtClassificationTypes.Keyword)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class RobotsTxtBoldFormatDefinition : ClassificationFormatDefinition
    {
        public RobotsTxtBoldFormatDefinition()
        {
            IsBold = true;
            DisplayName = "Robots.txt Keyword";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = RobotsTxtClassificationTypes.Comment)]
    [Name(RobotsTxtClassificationTypes.Comment)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class RobotsTxtHeaderFormatDefinition : ClassificationFormatDefinition
    {
        public RobotsTxtHeaderFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Green;
            IsItalic = true;
            DisplayName = "Robots.txt Comment";
        }
    }
}
