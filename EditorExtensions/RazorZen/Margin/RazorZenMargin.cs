using MadsKristensen.EditorExtensions.Margin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Linq;

namespace MadsKristensen.EditorExtensions.RazorZen
{
    internal class RazorZenMargin : TextViewMargin
    {
        public RazorZenMargin(ITextDocument document, IWpfTextView sourceView)
            : base(RazorZenContentTypeDefinition.RazorZenContentType, document, sourceView)
        { }
    }
}