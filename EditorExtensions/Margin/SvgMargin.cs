using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class SvgMargin : DirectMarginBase
    {
        private WebBrowser _browser;

        public SvgMargin(ITextDocument document)
            : base(WESettings.Instance.General, document)
        { }

        protected override void UpdateMargin(string sourcePath)
        {
            if (_browser != null && File.Exists(sourcePath))
            {
                _browser.Navigate(sourcePath);
            }
        }

        protected override FrameworkElement CreatePreviewControl(double width)
        {
            _browser = new WebBrowser();
            _browser.HorizontalAlignment = HorizontalAlignment.Stretch;
            return _browser;
        }
    }
}