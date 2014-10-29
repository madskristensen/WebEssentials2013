using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.JSON.Editor.Schema.Def;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("Property QuickInfo Controller")]
    [ContentType("JSON")]
    public class PropertyQuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        public IQuickInfoBroker QuickInfoBroker { get; set; }
        
        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new PropertyQuickInfoController(textView, subjectBuffers, this);
        }
    }
}
