using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(CssContentTypeDefinition.CssContentType)]
    [TagType(typeof(SmartTag))]
    internal class SmartTagProvider : IViewTaggerProvider
    {
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer textBuffer) where T : ITag
        {
            CssSmartTagger tagger = new CssSmartTagger(textView, textBuffer);
            return tagger as ITagger<T>;
        }
    }
}
