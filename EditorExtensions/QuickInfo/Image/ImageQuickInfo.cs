using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Web;

namespace MadsKristensen.EditorExtensions
{
    internal class ImageQuickInfo : IQuickInfoSource
    {
        private ITextBuffer _buffer;
        private static List<string> _imageExtensions = new List<string>() { ".png", ".jpg", "gif", ".jpeg", ".bmp", ".tif", ".tiff" };

        public ImageQuickInfo(ITextBuffer subjectBuffer)
        {
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (session == null || qiContent == null)
                return;

            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            var tree = CssEditorDocument.FromTextBuffer(_buffer);
            ParseItem item = tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            UrlItem urlItem = item.FindType<UrlItem>();

            if (urlItem != null && urlItem.UrlString != null && urlItem.UrlString.IsValid)
            {
                string url = GetFileName(urlItem.UrlString.Text.Trim('\'', '"'));
                if (!string.IsNullOrEmpty(url))
                {
                    applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(point.Value.Position, 1, SpanTrackingMode.EdgeNegative);
                    var image = CreateImage(url);
                    if (image != null && image.Source != null)
                    {
                        qiContent.Add(image);
                        qiContent.Add(Math.Round(image.Source.Width) + "x" + Math.Round(image.Source.Height));
                    }
                }
            }
        }
        
        public static string GetFileName(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (text.StartsWith("data:", StringComparison.Ordinal))
                    return text;

                string imageUrl = text.Trim(new[] { '\'', '"' });
                //if (!_imageExtensions.Contains(Path.GetExtension(imageUrl)))
                //    return null;
                
                string filePath = string.Empty;

                if (text.StartsWith("//", StringComparison.Ordinal))
                    text = "http:" + text;

                if (text.StartsWith("http://", StringComparison.Ordinal) || text.Contains(";base64,"))
                {
                    return text;
                }
                else if (imageUrl.StartsWith("/", StringComparison.Ordinal))
                {
                    imageUrl = HttpUtility.UrlDecode(imageUrl);
                    
                    string root = ProjectHelpers.GetProjectFolder(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
                    if (root.Contains("://"))
                        return root + imageUrl;
                    
                    if (!string.IsNullOrEmpty(root))
                        filePath = ProjectHelpers.ToAbsoluteFilePathFromActiveFile(imageUrl);// new FileInfo(root).Directory + imageUrl;
                }
                else if (EditorExtensionsPackage.DTE.ActiveDocument != null)
                {
                    imageUrl = HttpUtility.UrlDecode(imageUrl);
                    FileInfo fi = new FileInfo(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
                    DirectoryInfo dir = fi.Directory;
                    while (imageUrl.Contains("../"))
                    {
                        imageUrl = imageUrl.Remove(imageUrl.IndexOf("../", StringComparison.Ordinal), 3);
                        dir = dir.Parent;
                    }

                    filePath = Path.Combine(dir.FullName, imageUrl.Replace("/", "\\"));
                }

                return File.Exists(filePath) ? filePath : "pack://application:,,,/WebEssentials2013;component/Resources/nopreview.png";
            }

            return null;
        }

        public static Image CreateImage(string file)
        {
            try
            {
                var image = new Image();

                if (file.StartsWith("data:", StringComparison.Ordinal))
                {
                    int index = file.IndexOf("base64,", StringComparison.Ordinal) + 7;
                    byte[] imageBytes = Convert.FromBase64String(file.Substring(index));
                    
                    using (MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
                    {
                        image.Source = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                }
                else if (File.Exists(file))
                {
                    image.Source = BitmapFrame.Create(new Uri(file), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }

                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
