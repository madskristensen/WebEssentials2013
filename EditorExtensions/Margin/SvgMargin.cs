using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class SvgMargin : MarginBase
    {
        public const string MarginName = "SvgMargin";
        private WebBrowser _browser;
        private string _fileName;

        protected override bool CanWriteToDisk
        {
            get { return false; }
        }

        public SvgMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        {
            _fileName = document.FilePath;
        }

        protected override void StartCompiler(string source)
        {
            if (_browser != null && File.Exists(_fileName))
            {
                _browser.Navigate(_fileName);
            }
        }

        protected override void CreateControls(IWpfTextViewHost host, string source)
        {
            int width = WESettings.GetInt(SettingsKey);
            width = width == -1 ? 400 : width;

            _browser = new WebBrowser();
            _browser.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(width) });
            grid.RowDefinitions.Add(new RowDefinition());

            grid.Children.Add(_browser);
            this.Children.Add(grid);

            Grid.SetColumn(_browser, 2);
            Grid.SetRow(_browser, 0);

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
            Settings.SetValue(SettingsKey, (int)this.ActualWidth);
            Settings.Save();
        }

        protected override void MinifyFile(string fileName, string source)
        {
            // Nothing to minify
        }

        public override bool IsSaveFileEnabled
        {
            get { return false; }
        }

        public override bool CompileEnabled
        {
            get { return true; }
        }

        public override string CompileToLocation
        {
            get { return string.Empty; }
        }
    }
}