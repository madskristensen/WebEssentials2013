using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
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
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }
        [Import]
        public ITextUndoHistoryRegistry UndoRegistry { get; set; }


        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new EncodeSelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new SortSelectedLines(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new RemoveDuplicateLines(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new RemoveEmptyLines(textViewAdapter, textView));

            textView.Properties.GetOrCreateSingletonProperty(() => new CommandExceptionFilter(textViewAdapter, textView, UndoRegistry));

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                var saveListeners = Mef.GetAllImports<IFileSaveListener>(document.TextBuffer.ContentType);
                if (saveListeners.Count > 0)
                {
                    EventHandler<TextDocumentFileActionEventArgs> saveHandler = (s, e) =>
                    {
                        if (e.FileActionType != FileActionTypes.ContentSavedToDisk)
                            return;

                        foreach (var listener in saveListeners)
                            listener.FileSaved(document.TextBuffer.ContentType, e.FilePath, false, false);
                    };

                    document.FileActionOccurred += saveHandler;
                    textView.Closed += delegate { document.FileActionOccurred -= saveHandler; };
                }
            }
        }
    }
}
