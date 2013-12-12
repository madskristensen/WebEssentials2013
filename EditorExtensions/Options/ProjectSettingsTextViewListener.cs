using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Options
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("XML")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class ProjectSettingsTextViewListener : IWpfTextViewCreationListener
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        private void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk && e.FilePath.EndsWith(Settings._fileName, StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() =>
                {
                    Settings.UpdateCache();
                });
            }
        }
    }
}
