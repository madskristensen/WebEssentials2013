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
}
