using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows.Forms;
using MadsKristensen.EditorExtensions.Markdown;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Images
{
    internal class PasteImage : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        private static string _lastPath;

        public PasteImage(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd97CmdID.Paste)
        {
            WebEssentialsPackage.DTE.Events.SolutionEvents.AfterClosing += delegate { _lastPath = null; };
        }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            IDataObject data = Clipboard.GetDataObject();

            var formats = data.GetFormats();

            // This is to check if the image is text copied from PowerPoint etc. 
            bool trueBitmap = formats.Any(x => new[] { "DeviceIndependentBitmap", "PNG", "JPG", "System.Drawing.Bitmap" }.Contains(x));
            bool textFormat = formats.Any(x => new[] { "Text", "Rich Text Format" }.Contains(x));
            bool hasBitmap = data.GetDataPresent("System.Drawing.Bitmap") || data.GetDataPresent(DataFormats.FileDrop);

            if (!hasBitmap || !trueBitmap || textFormat)
                return false;

            string fileName = null;

            if (!GetFileName(data, out fileName))
                return true;

            _lastPath = Path.GetDirectoryName(fileName);

            SaveClipboardImageToFile(data, fileName);

            TextView.InsertLinkToImageFile(fileName);

            return true;
        }

        private static bool GetFileName(IDataObject data, out string fileName)
        {
            string extension = "png";
            fileName = "file";

            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string fullpath = ((string[])data.GetData(DataFormats.FileDrop))[0];
                fileName = Path.GetFileName(fullpath);
                extension = Path.GetExtension(fileName).TrimStart('.');
            }
            else
            {
                extension = GetMimeType((Bitmap)data.GetData("System.Drawing.Bitmap"));
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = fileName;
                dialog.DefaultExt = "." + extension;
                dialog.Filter = extension.ToUpperInvariant() + " Files|*." + extension;
                dialog.InitialDirectory = _lastPath ?? ProjectHelpers.GetRootFolder();

                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;

                fileName = dialog.FileName;
            }

            return true;
        }

        private static string GetMimeType(Bitmap bitmap)
        {
            if (bitmap.RawFormat.Guid == ImageFormat.Bmp.Guid)
                return "bmp";
            if (bitmap.RawFormat.Guid == ImageFormat.Emf.Guid)
                return "emf";
            if (bitmap.RawFormat.Guid == ImageFormat.Exif.Guid)
                return "exif";
            if (bitmap.RawFormat.Guid == ImageFormat.Gif.Guid)
                return "gif";
            if (bitmap.RawFormat.Guid == ImageFormat.Icon.Guid)
                return "icon";
            if (bitmap.RawFormat.Guid == ImageFormat.Jpeg.Guid)
                return "jpg";
            if (bitmap.RawFormat.Guid == ImageFormat.Tiff.Guid)
                return "tiff";
            if (bitmap.RawFormat.Guid == ImageFormat.Wmf.Guid)
                return "wmf";

            return "png";
        }

        private static async void SaveClipboardImageToFile(IDataObject data, string fileName)
        {
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string original = ((string[])data.GetData(DataFormats.FileDrop))[0];

                if (File.Exists(original))
                    File.Copy(original, fileName, true);
            }
            else
            {
                using (Bitmap image = (Bitmap)data.GetData("System.Drawing.Bitmap"))
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, ImageHelpers.GetImageFormatFromExtension(Path.GetExtension(fileName)));
                    byte[] buffer = ms.ToArray();
                    await FileHelpers.WriteAllBytesRetry(fileName, buffer);
                }
            }

            ImageCompressor compressor = new ImageCompressor();
            await compressor.CompressFilesAsync(fileName).HandleErrors("compressing " + fileName);

            ProjectHelpers.AddFileToActiveProject(fileName);
        }

        

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}