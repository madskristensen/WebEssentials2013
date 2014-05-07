using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Text;
using mshtml;

namespace MadsKristensen.EditorExtensions.Markdown
{
    internal class MarkdownMargin : CompilingMarginBase
    {
        private HTMLDocument _document;
        private WebBrowser _browser;
        private const string _stylesheet = "WE-Markdown.css";
        private double _cachedPosition = 0,
                       _cachedHeight = 0,
                       _positionPercentage = 0;

        public MarkdownMargin(ITextDocument document)
            : base(WESettings.Instance.Markdown, document)
        { }

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

            if (string.IsNullOrEmpty(folder))
                return null;

            return Path.Combine(folder, _stylesheet);
        }

        public async static Task CreateStylesheet()
        {
            string file = Path.Combine(ProjectHelpers.GetSolutionFolderPath(), _stylesheet);

            await FileHelpers.WriteAllTextRetry(file, "body { background: yellow; }");
            ProjectHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(file);
        }

        protected override void UpdateMargin(CompilerResult result)
        {
            if (_browser == null)
                return;
            // The Markdown compiler cannot return errors
            string html = String.Format(CultureInfo.InvariantCulture, @"<!DOCTYPE html>
                                        <html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
                                            <head>
                                                <meta charset=""utf-8"" />
                                                <base href=""file:///{0}/"">
                                                <title>Markdown Preview</title>
                                                {1}
                                            </head>
                                            <body>{2}</body>
                                        </html>",
                                        Path.GetDirectoryName(Document.FilePath).Replace("\\", "/"),
                                        GetStylesheet(),
                                        result.Result);

            if (_document == null)
            {
                _browser.NavigateToString(html);

                return;
            }

            _cachedPosition = _document.documentElement.getAttribute("scrollTop");
            _cachedHeight = Math.Max(1.0, _document.body.offsetHeight);
            _positionPercentage = _cachedPosition * 100 / _cachedHeight;

            _browser.NavigateToString(html);
        }

        protected override FrameworkElement CreatePreviewControl()
        {
            _browser = new WebBrowser();
            _browser.HorizontalAlignment = HorizontalAlignment.Stretch;
            _browser.Navigated += (s, e) =>
            {
                _document = _browser.Document as HTMLDocument;
                _cachedHeight = _document.body.offsetHeight;
                _document.documentElement.setAttribute("scrollTop", _positionPercentage * _cachedHeight / 100);
            };

            return _browser;
        }
    }
}