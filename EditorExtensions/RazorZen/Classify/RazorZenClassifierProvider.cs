using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.RazorZen
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(RazorZenContentTypeDefinition.RazorZenContentType)]
    public class RazorZenClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new RazorZenClassifier(Registry));
        }
    }
}