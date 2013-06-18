//using System.ComponentModel.Composition;
//using Microsoft.CSS.Editor;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Classification;
//using Microsoft.VisualStudio.Utilities;
//using Microsoft.Web.Editor;

//namespace MadsKristensen.EditorExtensions
//{
//    [Export(typeof(IClassifierProvider))]
//    [ContentType(ScssContentTypeDefinition.ScssContentType)]
//    internal sealed class ScssClassificationProvider : IClassifierProvider
//    {
//        [Import]
//        public IClassificationTypeRegistryService ClassificationRegistryService { get; set; }

//        public IClassifier GetClassifier(ITextBuffer textBuffer)
//        {
//            var classifier = ServiceManager.GetService<CssClassifier>(textBuffer);
            
//            if (classifier == null)
//                classifier = new CssClassifier(textBuffer, new CssClassificationNameProvider(), ClassificationRegistryService);

//            return classifier;
//        }
//    }
//}
