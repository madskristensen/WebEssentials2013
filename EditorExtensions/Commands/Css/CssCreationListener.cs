using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(CssContentTypeDefinition.CssContentType)]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class CssConnectionListener : IWpfTextViewConnectionListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public IClassifierAggregatorService AggregatorService { get; set; }

        [Import]
        public ICompletionBroker CompletionBroker { get; set; }

        [Import]
        public IQuickInfoBroker QuickInfoBroker { get; set; }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!subjectBuffers.Any(b => b.ContentType.IsOfType(CssContentTypeDefinition.CssContentType)))
                return;

            var textViewAdapter = EditorAdaptersFactoryService.GetViewAdapter(textView);

            textView.Properties.GetOrCreateSingletonProperty<CssSortProperties>(() => new CssSortProperties(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssExtractToFile>(() => new CssExtractToFile(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssAddMissingStandard>(() => new CssAddMissingStandard(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssAddMissingVendor>(() => new CssAddMissingVendor(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssRemoveDuplicates>(() => new CssRemoveDuplicates(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<MinifySelection>(() => new MinifySelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssFindReferences>(() => new CssFindReferences(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<F1Help>(() => new F1Help(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssSelectBrowsers>(() => new CssSelectBrowsers(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<RetriggerTarget>(() => new RetriggerTarget(textViewAdapter, textView, CompletionBroker));

            uint cssFormatProperties;
            ErrorHandler.ThrowOnFailure(EditorExtensionsPackage.PriorityCommandTarget.RegisterPriorityCommandTarget(0, new CssFormatProperties(textView), out cssFormatProperties));
            textView.Closed += delegate { ErrorHandler.ThrowOnFailure(EditorExtensionsPackage.PriorityCommandTarget.UnregisterPriorityCommandTarget(cssFormatProperties)); };
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }
    }
}
