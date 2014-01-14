using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class SvgMargin : MarginBase
    {
        private WebBrowser _browser;

        public SvgMargin(ITextDocument document)
            : base(WESettings.Instance.General, document)
        { }

        protected override void StartUpdatePreview(string source)
        {
            if (_browser != null && File.Exists(Document.FilePath))
            {
                _browser.Navigate(Document.FilePath);
            }
        }

        protected override FrameworkElement CreateControl(double width)
        {
            _browser = new WebBrowser();
            _browser.HorizontalAlignment = HorizontalAlignment.Stretch;
            return _browser;
        }
    }
}