using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using ErrorTask = Microsoft.VisualStudio.Shell.Task;

namespace MadsKristensen.EditorExtensions
{
    public abstract class MarginBase : DockPanel, IWpfTextViewMargin
    {
        readonly string _settingsKey;
        Control _previewControl;

        private bool _isDisposed = false;
        private Dispatcher _dispatcher;
        protected ITextDocument Document { get; private set; }

        protected IMarginSettings Settings { get; private set; }

        protected MarginBase(IMarginSettings settings, ITextDocument document)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _settingsKey = GetType().Name;
            Document = document;
            Settings = settings;

            Document.FileActionOccurred += Document_FileActionOccurred;

            if (settings.ShowPreviewPane)
            {
                _dispatcher.BeginInvoke(
                    new Action(CreateControls), DispatcherPriority.ApplicationIdle, null);
            }
        }
        void Document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                _dispatcher.BeginInvoke(new Action(() => {
                    _provider.Tasks.Clear();
                    StartUpdatePreview(File.ReadAllText(e.FilePath));
                }), DispatcherPriority.ApplicationIdle, null);
            }
        }

        protected abstract void StartUpdatePreview(string source);
        protected abstract Control CreateControl(double width);

        void CreateControls()
        {
            int width;
            using (var key = EditorExtensionsPackage.Instance.UserRegistryRoot)
            {
                var raw = key.GetValue("WE_" + _settingsKey);
                width = raw != null ? (int)raw : -1;
            }
            width = width == -1 ? 400 : width;


            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition());

            _previewControl = CreateControl(width);
            grid.Children.Add(_previewControl);
            this.Children.Add(grid);

            Grid.SetColumn(_previewControl, 2);
            Grid.SetRow(_previewControl, 0);

            GridSplitter splitter = new GridSplitter();
            splitter.Width = 5;
            splitter.ResizeDirection = GridResizeDirection.Columns;
            splitter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            splitter.DragCompleted += splitter_DragCompleted;

            grid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);
            Grid.SetRow(splitter, 0);

            StartUpdatePreview(Document.TextBuffer.CurrentSnapshot.GetText());
        }

        void splitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            using (var key = EditorExtensionsPackage.Instance.UserRegistryRoot)
            {
                key.SetValue("WE_" + _settingsKey, _previewControl.Width);
            }
        }


        #region IWpfTextViewMargin Members

        /// <summary>
        /// The <see cref="Sytem.Windows.FrameworkElement"/> that implements the visual representation
        /// of the margin.
        /// </summary>
        public System.Windows.FrameworkElement VisualElement
        {
            // Since this margin implements Canvas, this is the object which renders
            // the margin.
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        #endregion

        #region ITextViewMargin Members

        public double MarginSize
        {
            // Since this is a horizontal margin, its width will be bound to the width of the text view.
            // Therefore, its size is its height.
            get
            {
                ThrowIfDisposed();
                return this.ActualHeight;
            }
        }

        public bool Enabled
        {
            // The margin should always be enabled
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        /// <summary>
        /// Returns an instance of the margin if this is the margin that has been requested.
        /// </summary>
        /// <param name="marginName">The name of the margin requested</param>
        /// <returns>An instance of EditorMargin1 or null</returns>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return (marginName == GetType().Name) ? (IWpfTextViewMargin)this : null;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            if (disposing)
            {
                if (_previewTextHost != null)
                {
                    _previewTextHost.Close();
                }

                if (Document != null)
                    Document.FileActionOccurred -= Document_FileActionOccurred;

                if (_provider != null)
                {
                    _provider.Tasks.Clear();
                    _provider.Dispose();
                }
            }
        }
        #endregion
    }
    public abstract class TextViewMarginBase : MarginBase
    {
        private ErrorListProvider _provider;
        private IWpfTextViewHost _previewTextHost;

        protected ICompilingMarginSettings CompilationSettings { get; private set; }

        protected bool IsFirstRun { get; private set; }
        readonly string _contentType;
        protected TextViewMarginBase(string contentType, IMarginSettings settings, ITextDocument document)
            : base(settings, document)
        {
            IsFirstRun = true;
            _provider = new ErrorListProvider(EditorExtensionsPackage.Instance);
            _contentType = contentType;
        }

        protected override Control CreateControl(double width)
        {
            _previewTextHost = CreateTextViewHost(_contentType);
            _previewTextHost.TextView.VisualElement.MinWidth = width;
            _previewTextHost.TextView.VisualElement.HorizontalAlignment = HorizontalAlignment.Stretch;
            _previewTextHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            _previewTextHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            _previewTextHost.TextView.VisualElement.KeyDown += TextView_KeyUp;


            return _previewTextHost.HostControl;
        }

        private static IWpfTextViewHost CreateTextViewHost(string contentType)
        {
            var componentModel = ProjectHelpers.GetComponentModel();
            var service = componentModel.GetService<IContentTypeRegistryService>();
            var type = service.GetContentType(contentType);

            var textBufferFactory = componentModel.GetService<ITextBufferFactoryService>();
            var textViewFactory = componentModel.GetService<ITextEditorFactoryService>();
            var textRoles = contentType == "JavaScript" ? new[] { PredefinedTextViewRoles.Document } : new[] { PredefinedTextViewRoles.Document, PredefinedTextViewRoles.Interactive };
            ITextBuffer textBuffer = textBufferFactory.CreateTextBuffer(string.Empty, type);
            ITextViewRoleSet roles = textViewFactory.CreateTextViewRoleSet(textRoles);
            IWpfTextView textView = textViewFactory.CreateTextView(textBuffer, roles);
            IWpfTextViewHost host = textViewFactory.CreateTextViewHost(textView, false);

            return host;
        }


        void TextView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                Clipboard.SetText(_previewTextHost.TextView.TextBuffer.CurrentSnapshot.GetText(_previewTextHost.TextView.Selection.Start.Position.Position, _previewTextHost.TextView.Selection.End.Position.Position - _previewTextHost.TextView.Selection.Start.Position.Position));
            else if (e.Key == Key.PageDown)
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Down);
            else if (e.Key == Key.PageUp)
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Up);
            else if (e.Key == Key.Down)
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Down);
            else if (e.Key == Key.Up)
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Up);
            else if (e.Key == Key.Home)
                _previewTextHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(_previewTextHost.TextView.TextBuffer.CurrentSnapshot, 0, 0));
            else if (e.Key == Key.End)
                _previewTextHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(_previewTextHost.TextView.TextBuffer.CurrentSnapshot, _previewTextHost.TextView.TextBuffer.CurrentSnapshot.Length, 0));
        }

        public void SetText(string text)
        {
            if (!Settings.ShowPreviewPane)
                return;

            if (string.IsNullOrEmpty(text))
            {
                _previewTextHost.HostControl.Opacity = 0.3;
                return;
            }
            int position = _previewTextHost.TextView.TextViewLines.FirstVisibleLine.Extent.Start.Position;
            _previewTextHost.TextView.TextBuffer.SetText(text);

            try
            {
                _previewTextHost.HostControl.Opacity = 1;
                _previewTextHost.TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, _previewTextHost.TextView.TextSnapshot.GetLineNumberFromPosition(position));
                _previewTextHost.TextView.ViewScroller.ScrollViewportHorizontallyByPixels(-9999);
            }
            catch
            {
                // Threading issues when called from TypeScript
            }
        }

        protected override async void StartUpdatePreview(string source)
        {
            var result = await CompileAsync(source);
            if (!result.IsSuccess)
            {
                SetText("/*\r\n\r\nCompile Error. \r\nSee error list for details\r\n"
                      + string.Join("\r\n", result.Errors.Select(e => e.Message))
                      + "\r\n\r\n*/");
                IsFirstRun = false;
                return;
            }
            if (!IsFirstRun && CompilationSettings.CompileOnSave)
            {
                string targetFileName = GetTargetFileName(state, CompilationSettings.OutputDirectory);

                WriteCompiledFile(result, state, targetFileName);

                if (!string.IsNullOrEmpty(targetFileName))
                    MinifyFile(targetFileName, result.Result);
            }
                IsFirstRun = false;
        }
        protected abstract Task<CompilerResult> CompileAsync(string source);

        private string GetTargetFileName()
        {
            switch (Path.GetExtension(Document.FilePath).ToLowerInvariant())
            {
                case ".less":
                    return GetCompiledFileName(Document.FilePath, ".css", CompilationSettings.OutputDirectory);

                case ".scss":
                    return GetCompiledFileName(Document.FilePath, ".css", CompilationSettings.OutputDirectory);

                case ".coffee":
                case ".iced":
                case ".ts":
                    return GetCompiledFileName(Document.FilePath, ".js", CompilationSettings.OutputDirectory);

                case ".md":
                case ".mdown":
                case ".markdown":
                case ".mkd":
                case ".mkdn":
                case ".mdwn":
                case ".mmd":
                    return GetCompiledFileName(Document.FilePath, ".html", CompilationSettings.OutputDirectory);

                default: // For the Diff view
                    return null;
            }
        }

        protected abstract void MinifyFile(string fileName, string source);

        private void WriteCompiledFile(string content, string currentFileName, string fileName)
        {
            if (ProjectHelpers.CheckOutFileFromSourceControl(fileName)
             && FileHelpers.WriteFile(content, fileName))
            {
                ProjectHelpers.AddFileToProject(currentFileName, fileName);
            }
        }

        public static string GetCompiledFileName(string Document.FilePath, string compiledExtension, string customFolder)
        {
            string compiledFileName = Path.GetFileName(Path.ChangeExtension(Document.FilePath, compiledExtension));
            string sourceDir = Path.GetDirectoryName(Document.FilePath);
            string compiledDir;
            string rootDir = ProjectHelpers.GetRootFolder();

            if (rootDir == null || rootDir.Length == 0)
            {
                // Assuming a project is not loaded..
                return Path.Combine(sourceDir, compiledFileName);
            }

            if (!string.IsNullOrEmpty(customFolder) &&
                !customFolder.Equals("false", StringComparison.OrdinalIgnoreCase) &&
                !customFolder.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                if (customFolder.StartsWith("~/", StringComparison.OrdinalIgnoreCase) || customFolder.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    // Output path starts at the project root..
                    compiledDir = Path.Combine(rootDir, customFolder.Substring(customFolder.StartsWith("~/", StringComparison.OrdinalIgnoreCase) ? 2 : 1));
                }
                else
                {
                    // Output path is a relative path..
                    // NOTE: if the output path doesn't exist, it will be created below.
                    compiledDir = new DirectoryInfo(Path.Combine(sourceDir, customFolder)).FullName;
                }

                if (!Directory.Exists(compiledDir))
                {
                    Directory.CreateDirectory(compiledDir);
                }

                return Path.Combine(compiledDir, compiledFileName);
            }

            return Path.Combine(sourceDir, compiledFileName);
        }

        protected void CreateTask(CompilerError error)
        {
            ErrorTask task = new ErrorTask() {
                Line = error.Line,
                Column = error.Column,
                ErrorCategory = TaskErrorCategory.Error,
                Category = TaskCategory.Html,
                Document = error.FileName,
                Priority = TaskPriority.Low,
                Text = error.Message,
            };

            task.AddHierarchyItem();

            task.Navigate += task_Navigate;
            _provider.Tasks.Add(task);
        }

        private void task_Navigate(object sender, EventArgs e)
        {
            ErrorTask task = sender as ErrorTask;

            _provider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindPrimary));

            if (task.Column > 0)
            {
                var doc = (TextDocument)EditorExtensionsPackage.DTE.ActiveDocument.Object("textdocument");
                doc.Selection.MoveToLineAndOffset(task.Line, task.Column, false);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarginBase");
        }
    }
}
