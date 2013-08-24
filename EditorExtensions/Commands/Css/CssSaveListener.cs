using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
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

                ProjectHelpers.CheckOutFileFromSourceControl(minFile);
                using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(true)))
                {
                    writer.Write(content);
                }

                if (WESettings.GetBoolean(WESettings.Keys.CssEnableGzipping))
                    GzipFile(file, minFile, content);
            }
            catch
            {
                Logger.Log("Error minifying: " + file);
            }
        }

        public static void GzipFile(string file, string minFile, string content)
        {
            string gzipFile = minFile + ".gzip";
            ProjectHelpers.CheckOutFileFromSourceControl(gzipFile);
            byte[] gzipContent = Compress(content);
            File.WriteAllBytes(gzipFile, gzipContent);
            ProjectHelpers.AddFileToActiveProject(gzipFile);
            MarginBase.AddFileToProject(file, gzipFile);
        }

        public static byte[] Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    zip.Write(buffer, 0, buffer.Length);
                }

                ms.Position = 0;
                byte[] compressed = new byte[ms.Length];
                ms.Read(compressed, 0, compressed.Length);

                byte[] gzBuffer = new byte[compressed.Length + 4];
                System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
                System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
                return gzBuffer;
            }
        }
    }
}
