using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using MadsKristensen.EditorExtensions.Markdown;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Images
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// This is not in the Shared/Extensions folder because it seems too specific.
    /// </remarks>
    internal static class ImageHelpers
    {
        private static string GetFormat(ITextBuffer buffer = null)
        {
            // CSS
            if (buffer != null)
            {
                if (buffer.ContentType.IsOfType(CssContentTypeDefinition.CssContentType))
                    return "background-image: url('{0}');";

                if (buffer.ContentType.IsOfType("JavaScript") || buffer.ContentType.IsOfType("TypeScript"))
                    return "var img = new Image();"
                         + Environment.NewLine
                         + "img.src = \"{0}\";";

                if (buffer.ContentType.IsOfType("CoffeeScript"))
                    return "img = new Image()"
                         + Environment.NewLine
                         + "img.src = \"{0}\"";

                if (buffer.ContentType.IsOfType(MarkdownContentTypeDefinition.MarkdownContentType))
                    return "![{1}]({0})";
            }

            return "<img src=\"{0}\" alt=\"\" />";
        }

        private static string GetFormat(IWpfTextView textView)
        {
            var projection = textView.TextBuffer as IProjectionBuffer;

            if (projection != null)
            {
                var snapshotPoint = textView.Caret.Position.BufferPosition;

                var buffers = projection.SourceBuffers.Where(
                    s =>
                        s.ContentType.IsOfType("CSS")
                        || s.ContentType.IsOfType("JavaScript")
                        || s.ContentType.IsOfType("TypeScript")
                        || s.ContentType.IsOfType("CoffeeScript")
                        || s.ContentType.IsOfType(MarkdownContentTypeDefinition.MarkdownContentType));

                foreach (ITextBuffer buffer in buffers)
                {
                    var point = textView.BufferGraph.MapDownToBuffer(
                        snapshotPoint, 
                        PointTrackingMode.Negative, 
                        buffer, 
                        PositionAffinity.Predecessor
                    );

                    if (point.HasValue)
                    {
                        return GetFormat(buffer);
                    }
                }

                return GetFormat();
            }
            return GetFormat(textView.TextBuffer);
        }

        private static string GetRelativeEncodedUrl(string fileName)
        {
            var activeDocument = WebEssentialsPackage.DTE.ActiveDocument;
            if (activeDocument == null)
                return null;

            var baseFolder = activeDocument.FullName; 
            
            var result = FileHelpers.RelativePath(baseFolder, fileName);

            if (result.Contains("://"))
            {
                int index = result.IndexOf('/', 12);
                if (index > -1)
                    result = result.Substring(index);
            }
            // Here we should check if the result need encoding 
            //
            //      string encodedResult = HttpUtility.UrlPathEncode(result);
            //      if(string.Compare(encodedResult , result, StringComparison.OrdinalIgnoreCase) != 0)
            //          return string.Format(CultureInfo.InvariantCulture, "<{0}>", result);
            //      return result;
            //
            // and if it does simply enclose the value between <> but 
            // this confuse the current markdown parser.
            // Return the encoded url rappresentation until the current 
            // parser is replaced by the one in CommonMark.Net.
            return HttpUtility.UrlPathEncode(result);
        }

        /// <summary>
        /// </summary>
        /// <param name="extension">The extension must containt the dot. Eg: ".jpg".</param>
        /// <returns></returns>
        public static ImageFormat GetImageFormatFromExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;

                case ".gif":
                    return ImageFormat.Gif;

                case ".bmp":
                    return ImageFormat.Bmp;

                case ".ico":
                    return ImageFormat.Icon;
            }

            return ImageFormat.Png;
        }

        /// <summary>
        /// Insert a link (relative to the current document) into the text view buffer 
        /// using the content type to select the right format (HTML, CSS, Markdown, etc..).
        /// </summary>
        /// <param name="textView"></param>
        /// <param name="absoluteImageFilePath"></param>
        public static bool InsertLinkToImageFile(this IWpfTextView textView, string absoluteImageFilePath)
        {
            string relative = GetRelativeEncodedUrl(absoluteImageFilePath);
            if (relative == null)
                return false;

            string format = GetFormat(textView);
            int position = textView.Caret.Position.BufferPosition.Position;
            
            string text = string.Format(CultureInfo.InvariantCulture, format, relative, Path.GetFileName(absoluteImageFilePath));

            using (WebEssentialsPackage.UndoContext("Insert Image"))
            {
                textView.TextBuffer.Insert(position, text);

                try
                {
                    SnapshotSpan span = new SnapshotSpan(textView.TextBuffer.CurrentSnapshot, position, format.Length);
                    textView.Selection.Select(span, false);

                    WebEssentialsPackage.ExecuteCommand("Edit.FormatSelection");
                    textView.Selection.Clear();
                }
                catch
                {
                    // Try to format the selection. Some editors handle this differently, so try/catch
                }
            }
            return true;
        }
    }
}