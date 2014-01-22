using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("TypeScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class TypeScriptSortPropertiesViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        [Import]
        internal IClassifierAggregatorService AggregatorService;

        [Import(typeof(ITextStructureNavigatorSelectorService))]
        public ITextStructureNavigatorSelectorService Navigator { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new TypeScriptSmartIndent(textViewAdapter, textView, CompletionBroker));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentCompletionCommandTarget(textViewAdapter, textView, AggregatorService));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentIndentationCommandTarget(textViewAdapter, textView, AggregatorService, CompletionBroker));

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                var lintInvoker = new LintFileInvoker(
                    f => new LintReporter(new TsLintCompiler(), WESettings.Instance.TypeScript, f),
                    document
                );
                textView.Closed += (s, e) => lintInvoker.Dispose();

                textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => lintInvoker);
            }
        }
    }
}
