using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Commands
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class LintConfigEncodingFixViewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            ITextDocument document;

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out document))
            {
                string fileName = Path.GetFileName(document.FilePath);

                if (fileName != JsHintCompiler.ConfigFileName &&
                    fileName != TsLintCompiler.ConfigFileName &&
                    fileName != JsCodeStyleCompiler.ConfigFileName &&
                    fileName != CoffeeLintCompiler.ConfigFileName)
                    return;

                document.Encoding = new UTF8Encoding(false);
            }
        }
    }
}