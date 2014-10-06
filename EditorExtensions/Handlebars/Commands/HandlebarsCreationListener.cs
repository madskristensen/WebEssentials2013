using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Handlebars
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(HandlebarsContentTypeDefinition.HandlebarsContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class HandlebarsViewCreationListener : IWpfTextViewConnectionListener
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                textView.Properties.GetOrCreateSingletonProperty("HandlebarsCompilationNotifier", () =>
                    Mef.GetImport<ICompilationNotifierProvider>(ContentTypeManager.GetContentType("Handlebars"))
                    .GetCompilationNotifier(document));
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (textView.Properties.ContainsProperty("HandlebarsCompilationNotifier"))
            {
                ICompilationNotifier provider =
                    textView.Properties.GetProperty<ICompilationNotifier>("HandlebarsCompilationNotifier");
                provider.Dispose();
            }
        }
    }
}
