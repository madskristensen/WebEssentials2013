using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.QuickInfo
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Image HTML QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType("htmlx")]
    internal class ImageHtmlQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new ImageHtmlQuickInfo();
        }
    }
}
