using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.RazorZen
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(RazorZenContentTypeDefinition.RazorZenContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class RazorZenViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new CommentCommandTarget(textViewAdapter, textView, "#"));
        }
    }
}