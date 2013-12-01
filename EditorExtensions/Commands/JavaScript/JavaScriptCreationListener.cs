using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JavaScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class JavaScriptSortPropertiesViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import(typeof(ITextStructureNavigatorSelectorService))]
        public ITextStructureNavigatorSelectorService Navigator { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty<MinifySelection>(() => new MinifySelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<JavaScriptFindReferences>(() => new JavaScriptFindReferences(textViewAdapter, textView, Navigator));
            textView.Properties.GetOrCreateSingletonProperty<CssExtractToFile>(() => new CssExtractToFile(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<NodeModuleGoToDefinition>(() => new NodeModuleGoToDefinition(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<ReferenceTagGoToDefinition>(() => new ReferenceTagGoToDefinition(textViewAdapter, textView));

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                JsHintProjectRunner runner = new JsHintProjectRunner(document);
                textView.Closed += (s, e) => runner.Dispose();

                textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => runner);
            }
        }
    }
}

