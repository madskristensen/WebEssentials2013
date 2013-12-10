using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("Declaration QuickInfo Controller")]
    [ContentType("CSS")]
    public class DeclarationQuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        public IQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new DeclarationQuickInfoController(textView, subjectBuffers, this);
        }
    }
}
