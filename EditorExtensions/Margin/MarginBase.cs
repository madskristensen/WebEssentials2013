﻿using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    public abstract class MarginBase : DockPanel, IWpfTextViewMargin
    {
        private bool _isDisposed = false;
        private IWpfTextViewHost _viewHost;
        private string _marginName;
        protected string _settingsKey;
        private bool _showMargin;
        protected bool _isFirstRun = true;
        private Dispatcher _dispatcher;
        private ErrorListProvider _provider;

        protected MarginBase()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public MarginBase(string source, string name, string contentType, bool showMargin, ITextDocument document)
        {
            Document = document;
            _marginName = name;
            _settingsKey = _marginName + "_width";
            _showMargin = showMargin;
            _dispatcher = Dispatcher.CurrentDispatcher;
            _provider = new ErrorListProvider(EditorExtensionsPackage.Instance);

            Document.FileActionOccurred += Document_FileActionOccurred;

            if (showMargin)
            {
                _dispatcher.BeginInvoke(
                    new Action(() => Initialize(contentType, source)), DispatcherPriority.ApplicationIdle, null);
            }
        }

        protected virtual void Document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    _provider.Tasks.Clear();
                    StartCompiler(File.ReadAllText(e.FilePath));
                }), DispatcherPriority.ApplicationIdle, null);
            }
        }

        public abstract bool IsSaveFileEnabled { get; }
        public abstract bool UseCompiledFolder { get; }
        protected ITextDocument Document { get; set; }

        private void Initialize(string contentType, string source)
        {
            _viewHost = CreateTextViewHost(contentType);
            CreateControls(_viewHost, source);
            StartCompiler(source);
        }

        private IWpfTextViewHost CreateTextViewHost(string contentType)
        {
            var componentModel = ProjectHelpers.GetComponentModel();
            var service = componentModel.GetService<IContentTypeRegistryService>();
            var type = service.GetContentType(contentType);

            var textBufferFactory = componentModel.GetService<ITextBufferFactoryService>();
            var textViewFactory = componentModel.GetService<ITextEditorFactoryService>();

            ITextBuffer textBuffer = textBufferFactory.CreateTextBuffer(string.Empty, type);
            ITextViewRoleSet roles = textViewFactory.CreateTextViewRoleSet(PredefinedTextViewRoles.Interactive, PredefinedTextViewRoles.Document);
            IWpfTextView textView = textViewFactory.CreateTextView(textBuffer, roles);
            IWpfTextViewHost host = textViewFactory.CreateTextViewHost(textView, false);

            return host;
        }

        protected virtual void CreateControls(IWpfTextViewHost host, string source)
        {
            int width;

            using (var key = EditorExtensionsPackage.Instance.UserRegistryRoot)
            {
                var raw = key.GetValue("WE_" + _settingsKey);
                width = raw != null ? (int)raw : -1;
            }

            width = width == -1 ? 400 : width;

            host.TextView.VisualElement.MinWidth = width;
            host.TextView.VisualElement.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            host.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            host.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            host.TextView.VisualElement.KeyDown += VisualElement_KeyUp;

            //host.GetTextViewMargin(PredefinedMarginNames.BottomControl).VisualElement.Height = 0;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition());

            grid.Children.Add(host.HostControl);
            this.Children.Add(grid);

            Grid.SetColumn(host.HostControl, 2);
            Grid.SetRow(host.HostControl, 0);

            GridSplitter splitter = new GridSplitter();
            splitter.Width = 5;
            splitter.ResizeDirection = GridResizeDirection.Columns;
            splitter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            splitter.DragCompleted += splitter_DragCompleted;

            grid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);
            Grid.SetRow(splitter, 0);
        }

        void splitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            //Settings.SetValue(_settingsKey, (int)_viewHost.HostControl.ActualWidth);
            //Settings.Save();
            using (var key = EditorExtensionsPackage.Instance.UserRegistryRoot)
            {
                key.SetValue("WE_" + _settingsKey, (int)_viewHost.HostControl.ActualWidth);
            }
        }

        protected void VisualElement_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                Clipboard.SetText(_viewHost.TextView.TextBuffer.CurrentSnapshot.GetText(_viewHost.TextView.Selection.Start.Position.Position, _viewHost.TextView.Selection.End.Position.Position - _viewHost.TextView.Selection.Start.Position.Position));
            else if (e.Key == Key.PageDown)
                _viewHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Down);
            else if (e.Key == Key.PageUp)
                _viewHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Up);
            else if (e.Key == Key.Down)
                _viewHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Down);
            else if (e.Key == Key.Up)
                _viewHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Up);
            else if (e.Key == Key.Home)
                _viewHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(_viewHost.TextView.TextBuffer.CurrentSnapshot, 0, 0));
            else if (e.Key == Key.End)
                _viewHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(_viewHost.TextView.TextBuffer.CurrentSnapshot, _viewHost.TextView.TextBuffer.CurrentSnapshot.Length, 0));
        }

        public void SetText(string text)
        {
            if (!_showMargin)
                return;

            if (!string.IsNullOrEmpty(text))
            {
                int position = _viewHost.TextView.TextViewLines.FirstVisibleLine.Extent.Start.Position;
                using (var edit = _viewHost.TextView.TextBuffer.CreateEdit())
                {
                    edit.Replace(new Span(0, _viewHost.TextView.TextBuffer.CurrentSnapshot.Length), text);
                    edit.Apply();
                }

                try
                {
                    _viewHost.HostControl.Opacity = 1;
                    _viewHost.TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, _viewHost.TextView.TextSnapshot.GetLineNumberFromPosition(position));
                    _viewHost.TextView.ViewScroller.ScrollViewportHorizontallyByPixels(-9999);
                }
                catch
                {
                    // Threading issues when called from TypeScript
                }
            }
            else
            {
                _viewHost.HostControl.Opacity = 0.3;
            }
        }

        protected abstract void StartCompiler(string source);

        protected void OnCompilationDone(string result, string state)
        {
            bool isSuccess = !result.StartsWith("ERROR:");

            _dispatcher.BeginInvoke(new Action(() =>
            {
                if (isSuccess)
                {
                    SetText(result);

                    if (!_isFirstRun)
                    {
                        if (IsSaveFileEnabled)
                            WriteCompiledFile(result, state);

                        MinifyFile(state, result);
                    }
                }
                else
                {
                    result = result.Replace("ERROR:", string.Empty);
                    SetText("/*\r\n\r\nCompile Error. \r\nSee error list for details\r\n" + result + "\r\n\r\n*/");
                }

                _isFirstRun = false;
            }), DispatcherPriority.Normal, null);
        }

        public abstract void MinifyFile(string fileName, string source);

        protected void WriteCompiledFile(string content, string currentFileName)
        {
            string extension = Path.GetExtension(currentFileName);
            string fileName = null;

            switch (extension.ToLowerInvariant())
            {
                case ".less":
                case ".scss":
                    fileName = GetCompiledFileName(currentFileName, ".css", UseCompiledFolder);
                    break;

                case ".coffee":
                case ".ts":
                    fileName = GetCompiledFileName(currentFileName, ".js", UseCompiledFolder);
                    break;

                default: // For the Diff view
                    return;
            }

            bool fileExist = File.Exists(fileName);
            bool fileWritten = false;

            ProjectHelpers.CheckOutFileFromSourceControl(fileName);
            fileWritten = WriteFile(content, fileName, fileExist, fileWritten);

            if (!fileExist && fileWritten)
            {
                AddFileToProject(currentFileName, fileName);
            }
        }

        public static string GetCompiledFileName(string sourceFileName, string compiledExtension, bool useFolder)
        {
            string sourceExtension = Path.GetExtension(sourceFileName);
            string compiledFileName = Path.GetFileName(sourceFileName).Replace(sourceExtension, compiledExtension);
            string sourceDir = Path.GetDirectoryName(sourceFileName);

            if (useFolder)
            {
                string compiledDir = Path.Combine(sourceDir, compiledExtension.Replace(".min.", string.Empty).Replace(".", string.Empty));

                if (!Directory.Exists(compiledDir))
                {
                    Directory.CreateDirectory(compiledDir);
                }

                return Path.Combine(compiledDir, compiledFileName);
            }

            return Path.Combine(sourceDir, compiledFileName);
        }

        public static void AddFileToProject(string parentFileName, string fileName)
        {
            if (!File.Exists(fileName))
                return;

            var item = EditorExtensionsPackage.DTE.Solution.FindProjectItem(parentFileName);

            if (item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName))
            {
                if (item.ProjectItems != null && Path.GetDirectoryName(parentFileName) == Path.GetDirectoryName(fileName))
                {
                    // WAP
                    item.ProjectItems.AddFromFile(fileName);
                }
                else
                {   // Website
                    item.ContainingProject.ProjectItems.AddFromFile(fileName);
                }
            }
        }

        private bool WriteFile(string content, string fileName, bool fileExist, bool fileWritten)
        {
            try
            {
                if (fileExist || (!fileExist && CanWriteToDisk(content)))
                {
                    using (StreamWriter writer = new StreamWriter(fileName, false, new UTF8Encoding(true)))
                    {
                        writer.Write(content);
                        fileWritten = true;
                    }
                }
            }
            catch (Exception ex)
            {
                var error = new CompilerError
                {
                    FileName = Document.FilePath,
                    Column = 0,
                    Line = 0,
                    Message = "Could not write to " + Path.GetFileName(fileName)
                };

                CreateTask(error);

                Logger.Log(ex);
            }

            return fileWritten;
        }

        protected void CreateTask(CompilerError error)
        {
            ErrorTask task = new ErrorTask()
            {
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
            Task task = sender as Task;

            _provider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindPrimary));

            if (task.Column > 0)
            {
                var doc = (TextDocument)EditorExtensionsPackage.DTE.ActiveDocument.Object("textdocument");
                doc.Selection.MoveToLineAndOffset(task.Line, task.Column, false);
            }
        }

        protected abstract bool CanWriteToDisk(string source);

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarginBase");
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
            return (marginName == this._marginName) ? (IWpfTextViewMargin)this : null;
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
                if (_viewHost != null)
                {
                    _viewHost.Close();
                }

                Document.FileActionOccurred -= Document_FileActionOccurred;
                _provider.Tasks.Clear();
                _provider.Dispose();
            }
        }
        #endregion

    }
}
