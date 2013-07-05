using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Formatting;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("HTML")]
    [ContentType("HTMLX")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class HtmlViewCreationListener : IVsTextViewCreationListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty<ZenCoding>(() => new ZenCoding(textViewAdapter, textView, CompletionBroker));
        }
    }

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("HTMLX")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class HtmlxViewCreationListener : IVsTextViewCreationListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty<SurroundWith>(() => new SurroundWith(textViewAdapter, textView, CompletionBroker));
            textView.Properties.GetOrCreateSingletonProperty<ExpandSelection>(() => new ExpandSelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<ContactSelection>(() => new ContactSelection(textViewAdapter, textView));
            //textView.Properties.GetOrCreateSingletonProperty<EnterFormat>(() => new EnterFormat(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<MinifySelection>(() => new MinifySelection(textViewAdapter, textView));
        }
    }
}
