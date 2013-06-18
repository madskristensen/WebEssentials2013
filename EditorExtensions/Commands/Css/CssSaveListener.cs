using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Microsoft.Web.Editor.CssContentTypeDefinition.CssContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class CssSaveListener : IWpfTextViewCreationListener
    {
        private ITextDocument _document;

        public void TextViewCreated(IWpfTextView textView)
        {
            textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out _document);

            if (_document != null)
            {
                _document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.EnableCssMinification))
                return;

            if (e.FileActionType == FileActionTypes.ContentSavedToDisk && e.FilePath.EndsWith(".css"))
            {
                string minFile = e.FilePath.Insert(e.FilePath.Length - 3, "min.");

                if (File.Exists(minFile) && EditorExtensionsPackage.DTE.Solution.FindProjectItem(minFile) != null)
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
            if (file.EndsWith(".min.css"))
                return;

            try
            {
                string content = MinifyFileMenu.MinifyString(".css", File.ReadAllText(file));
                //Minifier minifier = new Minifier();
                //string content = minifier.MinifyStyleSheet(File.ReadAllText(file));

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
