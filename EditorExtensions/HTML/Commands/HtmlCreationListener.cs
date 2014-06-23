using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Formatting;

namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("HTML")]
    [ContentType("HTMLX")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class HtmlViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ICompletionBroker CompletionBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new ZenCoding(textViewAdapter, textView, CompletionBroker));

            textView.MouseHover += textView_MouseHover;
            textView.Closed += textView_Closed;
        }

        void textView_MouseHover(object sender, MouseHoverEventArgs e)
        {
            if (InspectMode.IsInspectModeEnabled)
            {
                var doc = WebEssentialsPackage.DTE.ActiveDocument;
                if (doc != null)
                {
                    InspectMode.Select(e.View.TextDataModel.DocumentBuffer.GetFileName(), e.Position);
                }
            }
        }

        private void textView_Closed(object sender, System.EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;
            view.MouseHover -= textView_MouseHover;
            view.Closed -= textView_Closed;
        }
    }

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("HTMLX")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class HtmlxViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ICompletionBroker CompletionBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            var formatter = ComponentLocatorForContentType<IEditorFormatterProvider, IComponentContentTypes>.ImportOne(HtmlContentTypeDefinition.HtmlContentType).Value;

            textView.Properties.GetOrCreateSingletonProperty<SurroundWith>(() => new SurroundWith(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<ExpandSelection>(() => new ExpandSelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<ContractSelection>(() => new ContractSelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<EnterFormat>(() => new EnterFormat(textViewAdapter, textView, formatter, CompletionBroker));
            textView.Properties.GetOrCreateSingletonProperty<MinifySelection>(() => new MinifySelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<HtmlGoToDefinition>(() => new HtmlGoToDefinition(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<HtmlFindAllReferences>(() => new HtmlFindAllReferences(textViewAdapter, textView));
        }
    }
}
