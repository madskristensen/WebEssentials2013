using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    // TODO: Remove this when the SASS editor is included in VS.
    public static class SassClassificationTypes
    {
        public const string Variable = "Sass_variable";

        [Export, Name(SassClassificationTypes.Variable)]
        public static ClassificationTypeDefinition SassClassificationMarkup { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = SassClassificationTypes.Variable)]
    [Name(SassClassificationTypes.Variable)]
    [Order(After = Priority.Default)]
    [UserVisible(true)]
    internal sealed class SassVariableFormatDefinition : ClassificationFormatDefinition
    {
        public SassVariableFormatDefinition()
        {
            ForegroundColor = System.Windows.Media.Colors.DodgerBlue;
            DisplayName = "Sass Variable";
        }
    }
}