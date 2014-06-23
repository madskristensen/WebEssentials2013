using System;
using System.IO;
using System.Windows.Media.Imaging;
using MadsKristensen.EditorExtensions.Images;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class OptimizeImageSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private UrlItem _url;

        public OptimizeImageSmartTagAction(ITrackingSpan span, UrlItem url)
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
            get { return _url.IsDataUri() ? "Optimize dataUri" : "Optimize Image"; }
        }

        public async override void Invoke()
        {
            if (string.IsNullOrEmpty(_url.UrlString.Text))
                return;

            ImageCompressor compressor = new ImageCompressor();
            string url = _url.UrlString.Text.Trim('"', '\'');

            if (_url.IsDataUri())
            {
                string dataUri = await compressor.CompressDataUriAsync(url);

                if (dataUri.Length < url.Length)
                {
                    using (WebEssentialsPackage.UndoContext("Optimize image"))
                    {
                        Span span = Span.FromBounds(_url.UrlString.Start, _url.UrlString.AfterEnd);
                        _span.TextBuffer.Replace(span, "'" + dataUri + "'");
                    }
                }
            }
            else
            {
                string selection = Uri.UnescapeDataString(url);
                string fileName = ProjectHelpers.ToAbsoluteFilePath(selection, _span.TextBuffer.GetFileName());

                if (string.IsNullOrEmpty(fileName) || !ImageCompressor.IsFileSupported(fileName) || !File.Exists(fileName))
                    return;

                await compressor.CompressFilesAsync(fileName);
            }
        }
    }
}
