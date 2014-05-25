using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JavaScript")]
    [ContentType("node.js")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class JavaScriptSortPropertiesViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import(typeof(ITextStructureNavigatorSelectorService))]
        public ITextStructureNavigatorSelectorService Navigator { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        public ICompletionBroker CompletionBroker { get; set; }

        [Import]
        internal IClassifierAggregatorService AggregatorService;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new MinifySelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new JavaScriptFindReferences(textViewAdapter, textView, Navigator));
            textView.Properties.GetOrCreateSingletonProperty(() => new ExtractToFile(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new NodeModuleGoToDefinition(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new ReferenceTagGoToDefinition(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentCompletionCommandTarget(textViewAdapter, textView, AggregatorService));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentIndentationCommandTarget(textViewAdapter, textView, AggregatorService, CompletionBroker));

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                if (ProjectHelpers.GetProjectItem(document.FilePath) == null)
                    return;

                var jsHintLintInvoker = new LintFileInvoker(f => new JavaScriptLintReporter(new JsHintCompiler(), f), document);
                textView.Closed += (s, e) => jsHintLintInvoker.Dispose();

                textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => jsHintLintInvoker);

                var jsCodeStyleLintInvoker = new LintFileInvoker(f => new JavaScriptLintReporter(new JsCodeStyleCompiler(), f), document);
                textView.Closed += (s, e) => jsCodeStyleLintInvoker.Dispose();

                textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => jsCodeStyleLintInvoker);
            }
        }
    }
}

