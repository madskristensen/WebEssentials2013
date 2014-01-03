using System;
using System.IO;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class TypeScriptMargin : MarginBase
    {
        public const string MarginName = "TypeScriptMargin";
        private string _fileName;
        private bool _isReady;
        private FileSystemWatcher _watcher;

        public TypeScriptMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        {
            _fileName = document.FilePath;
            SetupWatcher();
        }

        protected override void StartCompiler(string source)
        {
            if (IsFirstRun)
            {
                string file = Path.ChangeExtension(_fileName, ".js");
                UpdateMargin(file);
            }
        }

        private void SetupWatcher()
        {
            string file = Path.ChangeExtension(_fileName, ".js");

            _watcher = new FileSystemWatcher();
            _watcher.Path = Path.GetDirectoryName(file);
            _watcher.Filter = Path.GetFileName(file);
            _watcher.EnableRaisingEvents = true;
            _watcher.Created += FileTouched;
            _watcher.Changed += FileTouched;
        }

        private void FileTouched(object sender, FileSystemEventArgs e)
        {
            UpdateMargin(e.FullPath);
        }

        private void UpdateMargin(string jsFile)
        {
            if (!_isReady && !IsFirstRun)
            {
                // The check for _isReady is in place to deal with the issue where
                // FileSystemWatcher fires twice per file change.
                _isReady = true;
                return;
            }

            if (Document.FilePath.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase))
            {
                OnCompilationDone("// Preview not available for *.d.ts files", jsFile);
                _isReady = false;
            }
            else if (File.Exists(jsFile))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnCompilationDone(File.ReadAllText(jsFile), jsFile);
                    _isReady = false;
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
            }
            else
            {
                OnCompilationDone("// Not compiled to disk yet", jsFile);
                _isReady = false;
            }
        }

        public override void MinifyFile(string fileName, string source)
        {
            // Nothing to minify
        }

        public override bool IsSaveFileEnabled
        {
            get { return false; }
        }

        protected override bool CanWriteToDisk(string source)
        {
            return false;
        }

        public override bool CompileEnabled
        {
            get { return false; }
        }

        public override string CompileToLocation
        {
            get { return string.Empty; }
        }
    }
}