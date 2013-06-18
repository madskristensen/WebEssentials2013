using Microsoft.VisualStudio.Text;
using System;
using System.IO;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    internal class JsHintProjectRunner : IDisposable
    {
        private ITextDocument _document;
        private JsHintRunner _runner;
        private bool _isDisposed;

        public JsHintProjectRunner(ITextDocument document)
        {
            _document = document;
            _document.FileActionOccurred += DocumentSavedHandler;
            _runner = new JsHintRunner(_document.FilePath);

            if (WESettings.GetBoolean(WESettings.Keys.EnableJsHint))
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => _runner.RunCompiler()), DispatcherPriority.ApplicationIdle, null);
            }
        }

        private void DocumentSavedHandler(object sender, TextDocumentFileActionEventArgs e)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.EnableJsHint))
                return;

            ITextDocument document = (ITextDocument)sender;

            if (document.TextBuffer != null && !_isDisposed && e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => _runner.RunCompiler()), DispatcherPriority.ApplicationIdle, null);
            }
        }

        public static void RunOnAllFilesInProject()
        {
            string dir = ProjectHelpers.GetRootFolder();

            if (dir != null && Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir, "*.js", SearchOption.AllDirectories))
                {
                    JsHintRunner runner = new JsHintRunner(file);
                    runner.RunCompiler();
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                //_document.Dispose();
                _runner.Dispose();
            }

            _isDisposed = true;
        }
    }
}