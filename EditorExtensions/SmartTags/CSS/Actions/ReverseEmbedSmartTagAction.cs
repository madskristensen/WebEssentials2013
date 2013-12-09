using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
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
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/embed.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.ReverseEmbedSmartTagActionName; }
        }

        public override void Invoke()
        {
            string base64 = _url.UrlString.Text.Trim('\'', '"');
            string mimeType = FileHelpers.GetMimeTypeFromBase64(base64);
            string extension = FileHelpers.GetExtension(mimeType) ?? "png";

            var fileName = FileHelpers.ShowDialog(extension);

            if (!string.IsNullOrEmpty(fileName) && TrySaveFile(base64, fileName))
            {
                EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
                ReplaceUrlValue(fileName);
                EditorExtensionsPackage.DTE.UndoContext.Close();
            }
        }

        private void ReplaceUrlValue(string fileName)
        {
            string relative = FileHelpers.RelativePath(EditorExtensionsPackage.DTE.ActiveDocument.FullName, fileName);
            string urlValue = string.Format(CultureInfo.InvariantCulture, "url({0})", relative);
            _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), urlValue);
        }

        public static bool TrySaveFile(string base64, string filePath)
        {
            try
            {
                int index = base64.IndexOf("base64,", StringComparison.Ordinal) + 7;
                byte[] imageBytes = Convert.FromBase64String(base64.Substring(index));
                File.WriteAllBytes(filePath, imageBytes);
                ProjectHelpers.AddFileToActiveProject(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.ShowMessage(ex.Message, "Web Essentials " + ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}