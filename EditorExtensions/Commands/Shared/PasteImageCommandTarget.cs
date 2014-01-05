using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class PasteImage : CommandTargetBase
    {
        private string _format;

        public PasteImage(IVsTextView adapter, IWpfTextView textView, string format)
            : base(adapter, textView, typeof(VSConstants.VSStd97CmdID).GUID, 26)
        {
            _format = format;
        }

        protected override bool Execute(CommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            IDataObject data = Clipboard.GetDataObject();
            ProjectItem item = ProjectHelpers.GetActiveFile();

            if (!data.GetDataPresent(DataFormats.Bitmap) || string.IsNullOrEmpty(item.ContainingProject.FullName))
                return false;

            string fileName = null;

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = "file.png";
                dialog.DefaultExt = ".png";
                dialog.Filter = "Images|*.png;*.gif;*.jpg;*.bmp;";
                dialog.InitialDirectory = ProjectHelpers.GetRootFolder();

                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return true;

                fileName = dialog.FileName;
            }

            SaveClipboardImageToFile(data, fileName);
            UpdateTextBuffer(fileName);

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                ProjectHelpers.AddFileToActiveProject(fileName);

            }), DispatcherPriority.ApplicationIdle, null);

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