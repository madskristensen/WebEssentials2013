using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MadsKristensen.EditorExtensions.Compilers;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.Win32;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>A base class for all margins, providing  basic layout and resize functionality.</summary>
    public abstract class MarginBase : DockPanel, IWpfTextViewMargin
    {
        readonly string _settingsKey;
        FrameworkElement _previewControl;

        private bool _isDisposed = false;
        private Dispatcher _dispatcher;
        protected ITextDocument Document { get; private set; }

        protected IMarginSettings Settings { get; private set; }

        protected MarginBase(IMarginSettings settings, ITextDocument document)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _settingsKey = document.TextBuffer.ContentType.DisplayName + "Margin_width";
            Document = document;
            Settings = settings;

            if (settings.ShowPreviewPane)
            {
                _dispatcher.BeginInvoke(
                    new Action(CreateMarginControls), DispatcherPriority.ApplicationIdle, null);
            }
        }

        protected abstract FrameworkElement CreatePreviewControl();

        protected virtual void CreateMarginControls()
        {
            int width;
            using (var key = WebEssentialsPackage.Instance.UserRegistryRoot)
            {
                var raw = key.GetValue("WE_" + _settingsKey);
                width = raw is int ? (int)raw : -1;
            }
            width = width == -1 ? 400 : width;


            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(width, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition());

            _previewControl = CreatePreviewControl();

            if (_previewControl == null)
                return;

            grid.Children.Add(_previewControl);
            Children.Add(grid);

            Grid.SetColumn(_previewControl, 2);
            Grid.SetRow(_previewControl, 0);

            GridSplitter splitter = new GridSplitter();
            splitter.Width = 5;
            splitter.ResizeDirection = GridResizeDirection.Columns;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.DragCompleted += splitter_DragCompleted;

            grid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);
            Grid.SetRow(splitter, 0);
        }

        void splitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (double.IsNaN(_previewControl.ActualWidth)) return;
            using (var key = WebEssentialsPackage.Instance.UserRegistryRoot)
            {
                key.SetValue("WE_" + _settingsKey, _previewControl.ActualWidth, RegistryValueKind.DWord);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
        #region IWpfTextViewMargin Members

        /// <summary>
        /// The <see cref="Sytem.Windows.FrameworkElement"/> that implements the visual representation
        /// of the margin.
        /// </summary>
        public FrameworkElement VisualElement { get { return this; } }

        #endregion

        #region ITextViewMargin Members

        public double MarginSize
        {
            // Since this is a horizontal margin, its width will be bound to the width of the text view.
            // Therefore, its size is its height.
            get
            {
                ThrowIfDisposed();
                return ActualHeight;
            }
        }

        public bool Enabled { get { return true; } }

        /// <summary>
        /// Returns an instance of the margin if this is the margin that has been requested.
        /// </summary>
        /// <param name="marginName">The name of the margin requested</param>
        /// <returns>An instance of EditorMargin1 or null</returns>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return (marginName == GetType().Name) ? this : null;
        }


        ///<summary>Releases all resources used by the MarginBase.</summary>
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        ///<summary>Releases the unmanaged resources used by the MarginBase and optionally releases the managed resources.</summary>
        ///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
        #endregion
    }

    ///<summary>A base class for margins that use the original file contents rather than the output of a compiler.</summary>
    public abstract class DirectMarginBase : MarginBase
    {
        protected DirectMarginBase(IMarginSettings settings, ITextDocument document)
            : base(settings, document)
        {
            Document.FileActionOccurred += Document_FileActionOccurred;
        }

        private void Document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
                UpdateMargin(e.FilePath);
        }

        protected override void CreateMarginControls()
        {
            base.CreateMarginControls();
            Dispatcher.InvokeAsync(() => UpdateMargin(Document.FilePath));
        }

        ///<summary>Updates the contents of the margin.</summary>
        protected abstract void UpdateMargin(string filePath);


        ///<summary>Releases the unmanaged resources used by the DirectMarginBase and optionally releases the managed resources.</summary>
        ///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Document.FileActionOccurred -= Document_FileActionOccurred;
            }
            base.Dispose(disposing);
        }
    }
    ///<summary>A base class for margins that display the result of an <see cref="ICompilationNotifier"/>.</summary>
    public abstract class CompilingMarginBase : MarginBase
    {
        public ICompilationNotifier Notifier { get; private set; }
        protected CompilingMarginBase(IMarginSettings settings, ITextDocument document)
            : base(settings, document)
        {
            Notifier = Mef.GetImport<ICompilationNotifierProvider>(Document.TextBuffer.ContentType).GetCompilationNotifier(document);
            Notifier.CompilationReady += (s, e) => UpdateMargin(e.CompilerResult);
        }

        protected override void CreateMarginControls()
        {
            if (!Settings.ShowPreviewPane)
                return;

            base.CreateMarginControls();
            Dispatcher.InvokeAsync(() => Notifier.RequestCompilationResult(cached: true));
        }

        protected abstract void UpdateMargin(CompilerResult result);


        ///<summary>Releases the unmanaged resources used by the CompilingMarginBase and optionally releases the managed resources.</summary>
        ///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Notifier.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
