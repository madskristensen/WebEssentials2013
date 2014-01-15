using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Microsoft.Web.Editor.HtmlContentTypeDefinition.HtmlContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class HtmlSaveListener : IWpfTextViewCreationListener
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private ITextDocument _document;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                _document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (!WESettings.Instance.Html.AutoMinify)
                return;

            if (e.FileActionType == FileActionTypes.ContentSavedToDisk && e.FilePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                string minFile = e.FilePath.Insert(e.FilePath.Length - 4, "min.");

                if (File.Exists(minFile) && ProjectHelpers.GetProjectItem(minFile) != null)
                {
                    Task.Run(() =>
                    {
                        Minify(e.FilePath, minFile);
                    });
                }
            }
        }

        public static void Minify(string file, string minFile)
        {
            if (file.EndsWith(".min.html", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                string content = MinifyFileMenu.MinifyString(".html", File.ReadAllText(file));

                ProjectHelpers.CheckOutFileFromSourceControl(minFile);
                using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(true)))
                {
                    writer.Write(content);
                }
            }
            catch
            {
                Logger.Log("Error minifying: " + file);
            }
        }
    }
}
