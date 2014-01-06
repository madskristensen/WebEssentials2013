using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    // TODO: Remove this when the SASS editor is included in VS.
    [Export(typeof(IClassifierProvider))]
    [ContentType(SassContentTypeDefinition.SassContentType)]
    public class SassClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new SassClassifier(Registry));
        }
    }
}