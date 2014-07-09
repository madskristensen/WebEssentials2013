using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using MadsKristensen.EditorExtensions.IcedCoffeeScript;
using MadsKristensen.EditorExtensions.LiveScript;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.CoffeeScript
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(CoffeeContentTypeDefinition.CoffeeContentType)]
    [ContentType(IcedCoffeeScriptContentTypeDefinition.IcedCoffeeScriptContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class CoffeeScriptViewCreationListener : IWpfTextViewConnectionListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (subjectBuffers.Any(b => b.ContentType.IsOfType(LiveScriptContentTypeDefinition.LiveScriptContentType)))
                return;

            var textViewAdapter = EditorAdaptersFactoryService.GetViewAdapter(textView);

            textView.Properties.GetOrCreateSingletonProperty(() => new EnterIndentation(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentCommandTarget(textViewAdapter, textView, "#"));

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                var lintInvoker = new LintFileInvoker(
                    f => new LintReporter(new CoffeeLintCompiler(), WESettings.Instance.CoffeeScript, f),
                    document
                );
                textView.Closed += (s, e) => lintInvoker.Dispose();

                textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => lintInvoker);
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        { }
    }

}
