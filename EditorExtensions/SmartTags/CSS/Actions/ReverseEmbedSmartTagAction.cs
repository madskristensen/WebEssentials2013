using System;
using System.Globalization;
using System.IO;
using System.Text;
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
            string mimeType = GetMimeType(base64);
            string extension = GetExtension(mimeType) ?? "png";

            var fileName = ShowDialog(extension);

            if (!string.IsNullOrEmpty(fileName) && TrySaveFile(base64, fileName))
            {
                EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
                ReplaceUrlValue(fileName);
                EditorExtensionsPackage.DTE.UndoContext.Close();
            }
        }

        private static string ShowDialog(string extension)
        {
            var initialPath = Path.GetDirectoryName(EditorExtensionsPackage.DTE.ActiveDocument.FullName);

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = "file." + extension;
                dialog.DefaultExt = extension;
                dialog.Filter = extension.ToUpperInvariant() + " files | *." + extension;
                dialog.InitialDirectory = initialPath;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }

            return null;
        }

        private void ReplaceUrlValue(string fileName)
        {
            string relative = FileHelpers.RelativePath(EditorExtensionsPackage.DTE.ActiveDocument.FullName, fileName);
            string urlValue = string.Format(CultureInfo.InvariantCulture, "url({0})", relative);
            _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), urlValue);
        }

        private static bool TrySaveFile(string base64, string file)
        {
            try
            {
                int index = base64.IndexOf("base64,", StringComparison.Ordinal) + 7;
                byte[] imageBytes = Convert.FromBase64String(base64.Substring(index));
                File.WriteAllBytes(file, imageBytes);
                ProjectHelpers.AddFileToActiveProject(file);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return false;
            }
        }

        private static string GetMimeType(string base64)
        {
            int end = base64.IndexOf(";", StringComparison.Ordinal);

            if (end > -1)
            {
                return base64.Substring(5, end - 5);
            }

            return string.Empty;
        }

        private static string GetExtension(string mimeType)
        {
            switch (mimeType)
            {
                case "image/png":
                    return "png";

                case "image/jpg":
                case "image/jpeg":
                    return "jpg";

                case "image/gif":
                    return "gif";

                case "image/svg":
                    return "svg";

                case "font/x-woff":
                    return "woff";

                case "font/otf":
                    return "otf";

                case "application/vnd.ms-fontobject":
                    return "eot";

                case "application/octet-stream":
                    return "ttf";
            }

            return null;
        }
    }
}