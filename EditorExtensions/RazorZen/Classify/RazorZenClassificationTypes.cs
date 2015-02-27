using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.RazorZen
{
    public static class RazorZenClassificationTypes
    {
        public const string ZenTag = "RazorZen_ZenTag";
        public const string ZenAttributName = "RazorZen_ZenAttributName";
        public const string ZenAttributValue = "RazorZen_ZenAttributValue";

        public const string RazorStart = "RazorZen_RazorStart";

        [Export, Name(RazorZenClassificationTypes.ZenAttributName)]
        public static ClassificationTypeDefinition RazorZenClassificationZenAttributName { get; set; }

        [Export, Name(RazorZenClassificationTypes.ZenTag)]
        public static ClassificationTypeDefinition RazorZenClassificationZenTag { get; set; }

        [Export, Name(RazorZenClassificationTypes.ZenAttributValue)]
        public static ClassificationTypeDefinition RazorZenClassificationZenAttributValue { get; set; }

        [Export, Name(RazorZenClassificationTypes.RazorStart)]
        public static ClassificationTypeDefinition RazorZenClassificationRazorStart { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = RazorZenClassificationTypes.ZenTag)]
    [Name(RazorZenClassificationTypes.ZenTag)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class RazorZenZenTagsFormatDefinition : ClassificationFormatDefinition
    {
        public RazorZenZenTagsFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Brown;
            DisplayName = "RazorZen Zen Tags";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = RazorZenClassificationTypes.ZenAttributName)]
    [Name(RazorZenClassificationTypes.ZenAttributName)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class RazorZenZenAttibuteNamesFormatDefinition : ClassificationFormatDefinition
    {
        public RazorZenZenAttibuteNamesFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Red;
            DisplayName = "RazorZen Zen Attribute Names";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = RazorZenClassificationTypes.ZenAttributValue)]
    [Name(RazorZenClassificationTypes.ZenAttributValue)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class RazorZenZenAttributeValuesFormatDefinition : ClassificationFormatDefinition
    {
        public RazorZenZenAttributeValuesFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.Blue;
            DisplayName = "RazorZen Zen Attribute Values";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = RazorZenClassificationTypes.RazorStart)]
    [Name(RazorZenClassificationTypes.RazorStart)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class RazorZenRazorStartFormatDefinition : ClassificationFormatDefinition
    {
        public RazorZenRazorStartFormatDefinition()
        {
            BackgroundColor = System.Windows.Media.Colors.Yellow;
            DisplayName = "RazorZen Razor Start";
        }
    }
}