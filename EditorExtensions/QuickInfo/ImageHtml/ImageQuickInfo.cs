using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions
{
    internal class ImageHtmlQuickInfo : IQuickInfoSource
    {
        private static List<string> _imageExtensions = new List<string>() { ".png", ".jpg", "gif", ".jpeg", ".bmp", ".tif", ".tiff" };

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;
                    
            SnapshotPoint? point = session.GetTriggerPoint(session.TextView.TextBuffer.CurrentSnapshot);

            if (!point.HasValue)
                return;

            HtmlEditorTree tree = HtmlEditorDocument.FromTextView(session.TextView).HtmlEditorTree;    

            ElementNode node = null;
            AttributeNode attr = null;

            tree.GetPositionElement(point.Value.Position, out node, out attr);

            if (attr == null || (attr.Name != "href" && attr.Name != "src"))
                return;

            string url = GetFileName(attr.Value.Trim('\'', '"'));

            if (!string.IsNullOrEmpty(url))
            {
                applicableToSpan = session.TextView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(point.Value.Position, 1, SpanTrackingMode.EdgeNegative);
                var image = CreateImage(url);
                if (image != null && image.Source != null)
                {
                    qiContent.Add(image);
                    qiContent.Add(Math.Round(image.Source.Width) + "x" + Math.Round(image.Source.Height));
                }
            }
        }

        public static string GetFileName(string text)
        {
            try
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
                        string root = ProjectHelpers.GetProjectFolder(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
                        if (root.Contains("://"))
                            return root + imageUrl;
                        else if (!string.IsNullOrEmpty(root))
                            filePath = root + imageUrl;// new FileInfo(root).Directory + imageUrl;
                    }
                    else if (EditorExtensionsPackage.DTE.ActiveDocument != null)
                    {
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
            }
            catch
            { }

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
