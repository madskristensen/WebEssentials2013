using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(CssContentTypeDefinition.CssContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class CssSortPropertiesViewCreationListener : IVsTextViewCreationListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        internal IClassifierAggregatorService AggregatorService { get; set; }

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        [Import]
        internal IQuickInfoBroker QuickInfoBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

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
            EditorExtensionsPackage.PriorityCommandTarget.RegisterPriorityCommandTarget(0, new CssFormatProperties(textView), out cssFormatProperties);
            textView.Closed += delegate { EditorExtensionsPackage.PriorityCommandTarget.UnregisterPriorityCommandTarget(cssFormatProperties); };

            uint cssSpeedTyping;
            EditorExtensionsPackage.PriorityCommandTarget.RegisterPriorityCommandTarget(0, new SpeedTypingTarget(this, textViewAdapter, textView), out cssSpeedTyping);
            textView.Closed += delegate { EditorExtensionsPackage.PriorityCommandTarget.UnregisterPriorityCommandTarget(cssSpeedTyping); };
        }
    }
}
