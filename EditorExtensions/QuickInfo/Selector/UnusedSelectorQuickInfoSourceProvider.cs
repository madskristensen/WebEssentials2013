using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.QuickInfo.Selector
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Unused CSS Selector QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType("CSS")]
    internal class UnusedSelectorQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new UnusedSelectorQuickInfo(this, textBuffer);
        }
    }
}
