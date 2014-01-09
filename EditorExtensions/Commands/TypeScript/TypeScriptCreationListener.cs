using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
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

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty<TypeScriptSmartIndent>(() => new TypeScriptSmartIndent(textViewAdapter, textView, CompletionBroker));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentCompletionCommandTarget(textViewAdapter, textView, AggregatorService));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentIndentationCommandTarget(textViewAdapter, textView, AggregatorService));
        }
    }
}