using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public static class AppCacheClassificationTypes
    {
        public const string Keywords = "AppCache_markup";
        public const string Comment = "AppCache_Comment";

        [Export, Name(AppCacheClassificationTypes.Keywords)]
        public static ClassificationTypeDefinition AppCacheClassificationMarkup { get; set; }

        [Export, Name(AppCacheClassificationTypes.Comment)]
        public static ClassificationTypeDefinition AppCacheClassificationComment { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = AppCacheClassificationTypes.Keywords)]
    [Name(AppCacheClassificationTypes.Keywords)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class AppCacheKeywordsFormatDefinition : ClassificationFormatDefinition
    {
        public AppCacheKeywordsFormatDefinition()
        {
            IsBold = true;
            DisplayName = "AppCache Keyword";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = AppCacheClassificationTypes.Comment)]
    [Name(AppCacheClassificationTypes.Comment)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class AppCacheCommentFormatDefinition : ClassificationFormatDefinition
    {
        public AppCacheCommentFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Green;
            IsItalic = true;
            DisplayName = "AppCache Comment";
        }
    }
}
