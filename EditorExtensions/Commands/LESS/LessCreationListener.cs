using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [ContentType(LessContentTypeDefinition.LessContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class LessViewCreationListener : IWpfTextViewConnectionListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!subjectBuffers.Any(b => b.ContentType.IsOfType(LessContentTypeDefinition.LessContentType)))
                return;

            var textViewAdapter = EditorAdaptersFactoryService.GetViewAdapter(textView);
            if (textViewAdapter == null)
                return;

            textView.Properties.GetOrCreateSingletonProperty<LessExtractVariableCommandTarget>(() => new LessExtractVariableCommandTarget(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<LessExtractMixinCommandTarget>(() => new LessExtractMixinCommandTarget(textViewAdapter, textView));
        }
        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }
    }
}
