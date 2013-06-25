using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.QuickInfo
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("Image Html QuickInfo Controller")]
    [ContentType("htmlx")]
    internal class ImageHtmlQuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new ImageHtmlQuickInfoController(textView, subjectBuffers, this);
        }
    }
}
