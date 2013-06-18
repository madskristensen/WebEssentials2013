using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.QuickInfo
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Image QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType("CSS")]
    internal class ImageQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new ImageQuickInfo(textBuffer);
        }
    }
}
