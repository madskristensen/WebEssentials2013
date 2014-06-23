using System;
using System.Globalization;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class ReverseEmbedSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private UrlItem _url;

        public ReverseEmbedSmartTagAction(ITrackingSpan span, UrlItem url)
        {
            _span = span;
            _url = url;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/embed.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.ReverseEmbedSmartTagActionName; }
        }

        public async override void Invoke()
        {
            string base64 = _url.UrlString.Text.Trim('\'', '"');
            string mimeType = FileHelpers.GetMimeTypeFromBase64(base64);
            string extension = FileHelpers.GetExtension(mimeType) ?? "png";

            var fileName = FileHelpers.ShowDialog(extension);

            if (!string.IsNullOrEmpty(fileName) && await FileHelpers.SaveDataUriToFile(base64, fileName))
            {
                using (WebEssentialsPackage.UndoContext((DisplayText)))
                    ReplaceUrlValue(fileName);
            }
        }

        private void ReplaceUrlValue(string fileName)
        {
            string relative = FileHelpers.RelativePath(WebEssentialsPackage.DTE.ActiveDocument.FullName, fileName);
            string urlValue = string.Format(CultureInfo.InvariantCulture, "url({0})", relative);
            _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), urlValue);
        }
    }
}