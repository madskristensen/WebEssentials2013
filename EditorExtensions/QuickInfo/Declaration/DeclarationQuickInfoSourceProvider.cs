using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Declaration QuickInfo Source")]
    [Order(Before = "Selector QuickInfo Source")]
    [ContentType("CSS")]
    internal class DeclarationQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new DeclarationQuickInfo(textBuffer);
        }
    }
}
