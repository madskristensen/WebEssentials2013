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
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class ImageQuickInfo : IQuickInfoSource
    {
        private ITextBuffer _buffer;
        private CssTree _tree;

        public ImageQuickInfo(ITextBuffer subjectBuffer)
        {
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (!EnsureTreeInitialized() || session == null || qiContent == null)
                return;

            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            ParseItem item = _tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            UrlItem urlItem = item.FindType<UrlItem>();

            if (urlItem == null || urlItem.UrlString == null || !urlItem.UrlString.IsValid)
                return;

            string url = GetFileName(urlItem.UrlString.Text.Trim('\'', '"'), _buffer);
            if (string.IsNullOrEmpty(url))
                return;

            applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(point.Value.Position, 1, SpanTrackingMode.EdgeNegative);

            var image = CreateImage(url);
            qiContent.Add(image);

            if (image.Tag == null)
                qiContent.Add(Math.Round(image.Source.Width) + "×" + Math.Round(image.Source.Height));
            else
                qiContent.Add(image.Tag);
        }

        /// <summary>
        /// This must be delayed so that the TextViewConnectionListener
        /// has a chance to initialize the WebEditor host.
        /// </summary>
        public bool EnsureTreeInitialized()
        {
            if (_tree == null)// && WebEditor.GetHost(CssContentTypeDefinition.CssContentType) != null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
                    _tree = document.Tree;
                }
                catch (Exception)
                {
                }
            }

            return _tree != null;
        }

        public static string GetFileName(string text, ITextBuffer sourceBuffer)
        {
            return GetFileName(text, sourceBuffer.GetFileName() ?? EditorExtensionsPackage.DTE.ActiveDocument.FullName);
        }
        public static string GetFileName(string text, string sourceFilename)
        {
            if (!string.IsNullOrEmpty(text))
            {
                string imageUrl = text.Trim(new[] { '\'', '"' });

                if (imageUrl.StartsWith("data:", StringComparison.Ordinal))
                    return text;

                string filePath = string.Empty;

                if (text.StartsWith("//", StringComparison.Ordinal))
                    text = "http:" + text;

                if (text.Contains("://") || text.Contains(";base64,"))
                {
                    return text;
                }
                else
                {
                    if (String.IsNullOrEmpty(sourceFilename))
                        return null;

                    imageUrl = HttpUtility.UrlDecode(imageUrl);
                    filePath = ProjectHelpers.ToAbsoluteFilePath(imageUrl, sourceFilename);
                }

                return filePath;
            }

            return null;
        }

        public static Image CreateImage(string file)
        {
            var image = new Image();
            try
            {
                if (file.StartsWith("data:", StringComparison.Ordinal))
                {
                    int index = file.IndexOf("base64,", StringComparison.Ordinal) + 7;
                    byte[] imageBytes = Convert.FromBase64String(file.Substring(index));

                    using (MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
                    {
                        image.Source = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                    return image;
                }
                else if (file.Contains("://") || File.Exists(file))
                {
                    image.Source = BitmapFrame.Create(new Uri(file));
                    return image;
                }
                image.Tag = "File not found";
            }
            catch (Exception ex)
            {
                image.Tag = ex.Message;
            }
            image.Source = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/nopreview.png"), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return image;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
