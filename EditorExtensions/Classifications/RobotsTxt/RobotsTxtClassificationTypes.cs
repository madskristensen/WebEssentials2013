using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    class RobotsTxtClassificationTypes
    {
        public const string RobotsTxtKeyword = "robotstxt_keyword";
        public const string RobotsTxtComment = "robotstxt_comment";

        [Export, Name(RobotsTxtClassificationTypes.RobotsTxtKeyword), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition RobotsTxtClassificationBold = null;

        [Export, Name(RobotsTxtClassificationTypes.RobotsTxtComment), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal static ClassificationTypeDefinition RobotsTxtClassificationHeader = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = RobotsTxtClassificationTypes.RobotsTxtKeyword)]
    [Name(RobotsTxtClassificationTypes.RobotsTxtKeyword)]
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
    [ClassificationType(ClassificationTypeNames = RobotsTxtClassificationTypes.RobotsTxtComment)]
    [Name(RobotsTxtClassificationTypes.RobotsTxtComment)]
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
