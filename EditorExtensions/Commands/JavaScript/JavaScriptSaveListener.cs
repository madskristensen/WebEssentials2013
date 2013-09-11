using Microsoft.Ajax.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("JavaScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class JavaScriptSaveListener : IWpfTextViewCreationListener
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
            if (!WESettings.GetBoolean(WESettings.Keys.EnableJsMinification))
                return;

            ITextDocument document = (ITextDocument)sender;

            if (document.TextBuffer != null && e.FileActionType == FileActionTypes.ContentSavedToDisk && e.FilePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                string minFile = e.FilePath.Insert(e.FilePath.Length - 2, "min.");
                string bundleFile = e.FilePath + ".bundle";

                if (!File.Exists(bundleFile) && File.Exists(minFile) && EditorExtensionsPackage.DTE.Solution.FindProjectItem(minFile) != null)
                {
                    Task.Run(() =>
                    {
                        Minify(e.FilePath, minFile, false);
                    });
                }
            }
        }

        public static void Minify(string sourceFile, string minFile, bool isBundle)
        {
            if (sourceFile.EndsWith(".min.js"))
                return;

            try
            {
                CodeSettings settings = new CodeSettings()
                {
                    EvalTreatment = EvalTreatment.MakeImmediateSafe,
                    TermSemicolons = true,
                    PreserveImportantComments = WESettings.GetBoolean(WESettings.Keys.KeepImportantComments)
                };

                if (WESettings.GetBoolean(WESettings.Keys.GenerateJavaScriptSourceMaps))
                {
                    MinifyFileWithSourceMap(sourceFile, minFile, settings, isBundle);
                }
                else
                {
                    MinifyFile(sourceFile, minFile, settings, isBundle);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void MinifyFileWithSourceMap(string file, string minFile, CodeSettings settings, bool isBundle)
        {
            string mapPath = minFile + ".map";
            ProjectHelpers.CheckOutFileFromSourceControl(mapPath);

            using (TextWriter writer = new StreamWriter(mapPath, false, new UTF8Encoding(false)))
            using (V3SourceMap sourceMap = new V3SourceMap(writer))
            {
                settings.SymbolsMap = sourceMap;

                sourceMap.StartPackage(Path.GetFileName(minFile), Path.GetFileName(mapPath));

                // This fails when debugger is attached. Bug raised with Ron Logan
                MinifyFile(file, minFile, settings, isBundle);

                sourceMap.EndPackage();

                if (!isBundle)
                {
                    MarginBase.AddFileToProject(file, mapPath);
                }
            }
        }

        private static void MinifyFile(string file, string minFile, CodeSettings settings, bool isBundle)
        {
            Minifier minifier = new Minifier();

            if (!isBundle)
            {
                minifier.FileName = Path.GetFileName(file);
            }

            string content = minifier.MinifyJavaScript(File.ReadAllText(file), settings);

            if (WESettings.GetBoolean(WESettings.Keys.GenerateJavaScriptSourceMaps))
            {
                content += Environment.NewLine + "//# sourceMappingURL=" + Path.GetFileName(minFile) + ".map";
            }

            ProjectHelpers.CheckOutFileFromSourceControl(minFile);
            using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(true)))
            {
                writer.Write(content);
            }

            if (WESettings.GetBoolean(WESettings.Keys.JavaScriptEnableGzipping))
                CssSaveListener.GzipFile(file, minFile, content);
        }
    }
}
