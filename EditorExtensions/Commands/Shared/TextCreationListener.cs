using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Commands
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class TextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextUndoHistoryRegistry UndoRegistry { get; set; }


        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty<EncodeSelection>(() => new EncodeSelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<SortSelectedLines>(() => new SortSelectedLines(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<RemoveDuplicateLines>(() => new RemoveDuplicateLines(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<RemoveEmptyLines>(() => new RemoveEmptyLines(textViewAdapter, textView));

            textView.Properties.GetOrCreateSingletonProperty(() => new CommandExceptionFilter(textViewAdapter, textView, UndoRegistry));
        }
    }
}
