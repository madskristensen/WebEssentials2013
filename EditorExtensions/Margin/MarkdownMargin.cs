using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using MarkdownSharp;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class MarkdownMargin : MarginBase
    {
        private Markdown _compiler;
        private WebBrowser _browser;
        private const string _stylesheet = "WE-Markdown.css";

        public MarkdownMargin(string contentType, string source, ITextDocument document)
            : base(source, contentType, WESettings.Instance.Markdown, document)
        { }

        protected override void StartCompiler(string source)
        {
            if (_compiler == null)
                _compiler = CreateCompiler();

            string result = _compiler.Transform(source);

            if (_browser != null)
            {
                string html =
                    String.Format(CultureInfo.InvariantCulture, @"<!DOCTYPE html>
                                    <html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
                                    <head>
                                        <meta charset=""utf-8"" />
                                        <title>Markdown Preview</title>
                                        {0}
                                    </head>
                                    <body>{1}</body></html>", GetStylesheet(), result);

                _browser.NavigateToString(html);
            }

            // NOTE: Markdown files are always compiled for the Preview window.
            //       But, only saved to disk when the CompileEnabled flag is true.
            //       That is why the following if statement is not wrapping this whole method.
            if (IsSaveFileEnabled)
            {
                OnCompilationDone(result.Trim(), Document.FilePath);
            }
        }

        public static Markdown CreateCompiler()
        {
            return new Markdown(WESettings.Instance.Markdown);
        }

        public static string GetStylesheet()
        {
            string file = GetCustomStylesheetFilePath();

            if (File.Exists(file))
            {
                string linkFormat = "<link rel=\"stylesheet\" href=\"{0}\" />";
                return string.Format(CultureInfo.CurrentCulture, linkFormat, file);
            }

            return "<style>body{font: 1.1em 'Century Gothic'}</style>";
        }

        public static string GetCustomStylesheetFilePath()
        {
            string folder = ProjectHelpers.GetSolutionFolderPath();
            return Path.Combine(folder, _stylesheet);
        }

        public static void CreateStylesheet()
        {
            string file = Path.Combine(ProjectHelpers.GetSolutionFolderPath(), _stylesheet);
            File.WriteAllText(file, "body { background: yellow; }", new UTF8Encoding(true));
            ProjectHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(file);
        }

        protected override void CreateControls(IWpfTextViewHost host, string source)
        {
            double width = WESettings.Instance.Markdown.PreviewPaneWidth;

            _browser = new WebBrowser();
            _browser.HorizontalAlignment = HorizontalAlignment.Stretch;

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
            WESettings.Instance.Markdown.PreviewPaneWidth = this.ActualWidth;
            SettingsStore.Save();
        }

        protected override void MinifyFile(string fileName, string source)
        {
            if (WESettings.Instance.Html.MinifyOnSave
                && File.Exists(Path.ChangeExtension(fileName, ".min.html")))
            {
                FileHelpers.MinifyFile(fileName, source, ".html");
            }
        }

        public override bool IsSaveFileEnabled
        {
            get { return WESettings.Instance.Markdown.CompileOnSave; }
        }

        public override string CompileToLocation
        {
            get { return WESettings.Instance.Markdown.OutputDirectory; }
        }
    }
}