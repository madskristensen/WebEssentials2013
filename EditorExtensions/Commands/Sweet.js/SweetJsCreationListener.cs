using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using MadsKristensen.EditorExtensions.Classifications.Sweet.js;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class SweetJsCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new EnterIndentation(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentCommandTarget(textViewAdapter, textView, "//"));
        }
    }
}
