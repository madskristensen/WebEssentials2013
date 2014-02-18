using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(CoffeeContentTypeDefinition.CoffeeContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class CoffeeScriptViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

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
    }

}
