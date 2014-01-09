using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class TsLintProjectRunner : IDisposable
    {
        private ITextDocument _document;
        private TsLintRunner _runner;
        private bool _isDisposed;

        public TsLintProjectRunner(ITextDocument document)
        {
            _document = document;
            _document.FileActionOccurred += DocumentSavedHandler;
            _runner = new TsLintRunner(_document.FilePath);

            if (WESettings.GetBoolean(WESettings.Keys.EnableTsLint))
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(_runner.RunCompiler), DispatcherPriority.ApplicationIdle, null);
            }
        }

        private void DocumentSavedHandler(object sender, TextDocumentFileActionEventArgs e)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.EnableTsLint))
                return;

            ITextDocument document = (ITextDocument)sender;
            if (_isDisposed || document.TextBuffer == null)
                return;

            switch (e.FileActionType)
            {
                case FileActionTypes.ContentLoadedFromDisk:
                    break;
                case FileActionTypes.DocumentRenamed:
                    _runner.Dispose();
                    _runner = new TsLintRunner(_document.FilePath);

                    goto case FileActionTypes.ContentSavedToDisk;
                case FileActionTypes.ContentSavedToDisk:
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(_runner.RunCompiler), DispatcherPriority.ApplicationIdle, null);
                    break;
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static void RunOnAllFilesInProject()
        {
            string dir = ProjectHelpers.GetRootFolder();

            if (dir != null && Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir, "*.ts", SearchOption.AllDirectories))
                {
                    TsLintRunner runner = new TsLintRunner(file);
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