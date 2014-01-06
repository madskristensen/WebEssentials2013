using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using EnvDTE;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class PasteImage : CommandTargetBase
    {
        private string _format;
        private static string _lastPath;

        public PasteImage(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(VSConstants.VSStd97CmdID).GUID, 26)
        {
            EditorExtensionsPackage.DTE.Events.SolutionEvents.AfterClosing += delegate { _lastPath = null; };
        }

        protected override bool Execute(CommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            IDataObject data = Clipboard.GetDataObject();
            ProjectItem item = ProjectHelpers.GetActiveFile();

            if (!data.GetDataPresent(DataFormats.Bitmap) || string.IsNullOrEmpty(item.ContainingProject.FullName))
                return false;

            if (!IsValidTextBuffer())
                return false;

            string fileName = null;

            if (!GetFileName(out fileName))
                return true;

            _lastPath = Path.GetDirectoryName(fileName);

            SaveClipboardImageToFile(data, fileName);
            UpdateTextBuffer(fileName);

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                ProjectHelpers.AddFileToActiveProject(fileName);

            }), DispatcherPriority.ApplicationIdle, null);

            return true;
        }

        private bool IsValidTextBuffer()
        {
            var projection = TextView.TextBuffer as IProjectionBuffer;

            if (projection != null)
            {
                var snapshotPoint = TextView.Caret.Position.BufferPosition;

                var buffers = projection.SourceBuffers.Where(
                    s =>
                        !s.ContentType.IsOfType("html")
                        && !s.ContentType.IsOfType("htmlx")
                        && !s.ContentType.IsOfType("inert")
                        && !s.ContentType.IsOfType("CSharp")
                        && !s.ContentType.IsOfType("VisualBasic")
                        && !s.ContentType.IsOfType("RoslynCSharp")
                        && !s.ContentType.IsOfType("RoslynVisualBasic")
                        || s.ContentType.IsOfType("Markdown"));

                foreach (ITextBuffer buffer in buffers)
                {
                    SnapshotPoint? point = TextView.BufferGraph.MapDownToBuffer(snapshotPoint, PointTrackingMode.Negative, buffer, PositionAffinity.Predecessor);

                    if (point.HasValue)
                    {
                        _format = GetFormat(buffer);
                        return true;
                    }
                }

                _format = GetFormat(null);
                return true;
            }
            else
            {
                _format = GetFormat(TextView.TextBuffer);
                return true;
            }
        }

        private static string GetFormat(ITextBuffer buffer)
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
                    return "![alt text]({0});";
            }

            return "<img src=\"{0}\" alt=\"\" />";
        }

        private static bool GetFileName(out string fileName)
        {
            fileName = null;

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = "file.png";
                dialog.DefaultExt = ".png";
                dialog.Filter = "Images|*.png;*.gif;*.jpg;*.bmp;";
                dialog.InitialDirectory = _lastPath ?? ProjectHelpers.GetRootFolder();

                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;

                fileName = dialog.FileName;
            }

            return true;
        }

        private void UpdateTextBuffer(string fileName)
        {
            string relative = "/" + FileHelpers.RelativePath(ProjectHelpers.GetRootFolder(), fileName);
            int position = TextView.Caret.Position.BufferPosition.Position;

            using (EditorExtensionsPackage.UndoContext("Insert Image"))
            {
                string text = string.Format(CultureInfo.InvariantCulture, _format, relative);
                TextView.TextBuffer.Insert(position, text);
            }
        }

        public static void SaveClipboardImageToFile(IDataObject data, string fileName)
        {
            using (Bitmap image = (Bitmap)data.GetData(DataFormats.Bitmap))
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, GetImageFormat(Path.GetExtension(fileName)));
                byte[] buffer = ms.ToArray();
                File.WriteAllBytes(fileName, buffer);
            }
        }

        private static ImageFormat GetImageFormat(string extension)
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

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}