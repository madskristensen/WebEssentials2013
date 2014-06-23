using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class EmbedSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private UrlItem _url;

        public EmbedSmartTagAction(ITrackingSpan span, UrlItem url)
        {
            _span = span;
            _url = url;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/embed.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.UrlSmartTagActionName; }
        }

        public override void Invoke()
        {
            string selection = Uri.UnescapeDataString(_url.UrlString.Text);

            if (selection != null)
            {
                string filePath = ProjectHelpers.ToAbsoluteFilePath(selection, _span.TextBuffer.GetFileName());
                ApplyChanges(filePath);
            }
        }

        private async void ApplyChanges(string filePath)
        {
            ITextSnapshot snapshot = _span.TextBuffer.CurrentSnapshot;

            if (File.Exists(filePath))
            {
                string dataUri = "url('" + await FileHelpers.ConvertToBase64(filePath) + "') /*" + Uri.UnescapeDataString(_url.UrlString.Text.Trim('"', '\'')) + "*/";
                InsertEmbedString(snapshot, dataUri);
            }
            else
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.CheckFileExists = true;
                    dialog.Multiselect = false;
                    dialog.InitialDirectory = new FileInfo(WebEssentialsPackage.DTE.ActiveDocument.FullName).Directory.FullName;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string dataUri = "url('" + await FileHelpers.ConvertToBase64(dialog.FileName) + "')";
                        InsertEmbedString(snapshot, dataUri);
                    }
                }
            }
        }

        private void InsertEmbedString(ITextSnapshot snapshot, string dataUri)
        {
            using (WebEssentialsPackage.UndoContext((DisplayText)))
            {
                _span.TextBuffer.Replace(_span.GetSpan(snapshot), dataUri);
            }
        }
    }
}
