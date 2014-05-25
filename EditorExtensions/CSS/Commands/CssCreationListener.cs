using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(CssContentTypeDefinition.CssContentType)]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class CssConnectionListener : IWpfTextViewConnectionListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!subjectBuffers.Any(b => b.ContentType.IsOfType(CssContentTypeDefinition.CssContentType)))
                return;

            var textViewAdapter = EditorAdaptersFactoryService.GetViewAdapter(textView);
            if (textViewAdapter == null)
                return;

            textView.Properties.GetOrCreateSingletonProperty<CssSortProperties>(() => new CssSortProperties(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<ExtractToFile>(() => new ExtractToFile(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssAddMissingStandard>(() => new CssAddMissingStandard(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssAddMissingVendor>(() => new CssAddMissingVendor(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssRemoveDuplicates>(() => new CssRemoveDuplicates(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<MinifySelection>(() => new MinifySelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssFindReferences>(() => new CssFindReferences(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<F1Help>(() => new F1Help(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<CssSelectBrowsers>(() => new CssSelectBrowsers(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<RetriggerTarget>(() => new RetriggerTarget(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<ArrowsCommandTarget>(() => new ArrowsCommandTarget(textViewAdapter, textView));

            CssSchemaUpdater.CheckForUpdates();
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }
    }
}
