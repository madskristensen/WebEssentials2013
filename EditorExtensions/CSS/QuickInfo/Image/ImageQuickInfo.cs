using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
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

            string url = GetFullUrl(urlItem.UrlString.Text.Trim('\'', '"'), _buffer);
            if (string.IsNullOrEmpty(url))
                return;

            applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(point.Value.Position, 1, SpanTrackingMode.EdgeNegative);

            AddImageContent(qiContent, url);
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

        public static string GetFullUrl(string text, ITextBuffer sourceBuffer)
        {
            return GetFullUrl(text, sourceBuffer.GetFileName() ?? WebEssentialsPackage.DTE.ActiveDocument.FullName);
        }
        public static string GetFullUrl(string text, string sourceFilename)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            text = text.Trim(new[] { '\'', '"', '~' });

            if (text.StartsWith("//", StringComparison.Ordinal))
                text = "http:" + text;

            if (text.Contains("://") || text.StartsWith("data:", StringComparison.Ordinal))
                return text;

            if (String.IsNullOrEmpty(sourceFilename))
                return null;

            text = HttpUtility.UrlDecode(text);
            return ProjectHelpers.ToAbsoluteFilePath(text, sourceFilename);
        }

        private static BitmapFrame LoadImage(string url)
        {
            try
            {
                if (url.StartsWith("data:", StringComparison.Ordinal))
                {
                    int index = url.IndexOf("base64,", StringComparison.Ordinal) + 7;
                    byte[] imageBytes = Convert.FromBase64String(url.Substring(index));

                    using (MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
                    {
                        // Must cache OnLoad before the stream is disposed
                        return BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                }
                else if (url.Contains("://") || File.Exists(url))
                {
                    return BitmapFrame.Create(new Uri(url), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
            }
            catch { }

            return null;
        }

        static T Freeze<T>(T obj) where T : Freezable { obj.Freeze(); return obj; }

        static readonly BitmapFrame noPreview = Freeze(BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/nopreview.png")));
        public static void AddImageContent(IList<object> qiContent, string url)
        {
            BitmapSource source;
            try
            {
                source = LoadImage(url);
            }
            catch (Exception ex)
            {
                qiContent.Add(new Image { Source = noPreview });
                qiContent.Add(ex.Message);
                return;
            }

            if (source == null)
            {
                qiContent.Add(new Image { Source = noPreview });
                qiContent.Add("Couldn't locate " + url);
                return;
            }

            // HWNDs are always 32-bit.
            // https://twitter.com/Schabse/status/406159104697049088
            // http://msdn.microsoft.com/en-us/library/aa384203.aspx
            var screen = Screen.FromHandle(new IntPtr(WebEssentialsPackage.DTE.ActiveWindow.HWnd));
            Image image = new Image
            {
                Source = source,
                MaxWidth = screen.WorkingArea.Width / 2,
                MaxHeight = screen.WorkingArea.Height / 2,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly
            };
            qiContent.Add(image);

            // Use a TextBuffer to show dynamic text with
            // the correct default styling. The presenter
            // uses the same technique to show strings in
            // QuickInfoItemView.CreateTextBuffer().
            // Base64Tagger assumes that text from base64
            // images will never change. If that changes,
            // you must change that to handle changes.
            var size = WebEditor.ExportProvider.GetExport<ITextBufferFactoryService>().Value.CreateTextBuffer();
            size.SetText("Loading...");

            source.OnDownloaded(() => size.SetText(Math.Round(source.Width) + "×" + Math.Round(source.Height)));
            if (source.IsDownloading)
            {
                EventHandler<System.Windows.Media.ExceptionEventArgs> failure = (s, e) =>
                {
                    image.Source = noPreview;
                    size.SetText("Couldn't load image: " + e.ErrorException.Message);
                };
                source.DecodeFailed += failure;
                source.DownloadFailed += failure;
            }

            qiContent.Add(size);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
