using EnvDTE;
using EnvDTE80;
using MarkdownSharp;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MadsKristensen.EditorExtensions
{
    internal class MarkdownMargin : MarginBase
    {
        public const string MarginName = "MarkdownMargin";
        private Markdown _compiler;
        private WebBrowser _browser;
        private const string _stylesheet = "WE-Markdown.css";

        public MarkdownMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        {
        }

        private void InitializeCompiler()
        {
            if (_compiler == null)
            {
                MarkdownOptions options = new MarkdownOptions();
                options.AutoHyperlink = true;

                _compiler = new Markdown(options);
            }
        }

        protected override void StartCompiler(string source)
        {
            InitializeCompiler();

            string result = _compiler.Transform(source);

            string html = "<html><head><meta charset=\"utf-8\" />" +
                          GetStylesheet() +
                          "</head>" +
                          "<body>" + result + "</body></html>";

            _browser.NavigateToString(html);
        }

        public static string GetStylesheet()
        {
            string folder = ProjectHelpers.GetSolutionFolderPath();

            if (!string.IsNullOrEmpty(folder))
            {
                string file = Path.Combine(folder, _stylesheet);

                if (File.Exists(file))
                {
                    string linkFormat = "<link rel=\"stylesheet\" href=\"{0}\" />";
                    return string.Format(linkFormat, file);
                }
            }

            return string.Empty;
        }

        public static void CreateStylesheet()
        {
            string file = Path.Combine(ProjectHelpers.GetSolutionFolderPath(), _stylesheet);

            using (StreamWriter writer = new StreamWriter(file, false, new UTF8Encoding(true)))
            {
                writer.Write("body { background: yellow; }");
            }

            Solution2 solution = EditorExtensionsPackage.DTE.Solution as Solution2;
            Project project = solution.Projects
                                .OfType<Project>()
                                .FirstOrDefault(p => p.Name.Equals(Settings._solutionFolder, StringComparison.OrdinalIgnoreCase));

            if (project == null)
            {
                project = solution.AddSolutionFolder(Settings._solutionFolder);
            }

            project.ProjectItems.AddFromFile(file);
        }

        protected override void CreateControls(IWpfTextViewHost host, string source)
        {
            int width = WESettings.GetInt(_settingsKey);
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
            Settings.SetValue(_settingsKey, (int)this.ActualWidth);
            Settings.Save();
        }

        public override void MinifyFile(string fileName, string source)
        {
            // Nothing to minify
        }

        public override bool UseCompiledFolder
        {
            get { return false; }
        }

        public override bool IsSaveFileEnabled
        {
            get { return false; }
        }

        protected override bool CanWriteToDisk(string source)
        {
            return false;
        }
    }
}