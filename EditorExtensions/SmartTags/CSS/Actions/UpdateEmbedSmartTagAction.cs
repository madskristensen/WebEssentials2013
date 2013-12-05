using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions
{
    internal class UpdateEmbedSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private UrlItem _url;
        private string _path;

        public UpdateEmbedSmartTagAction(ITrackingSpan span, UrlItem url, string path)
        {
            _span = span;
            _url = url;
            _path = path;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/embed.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return string.Format(Resources.UpdateEmbedSmartTagActionName, _path); }
        }

        public override void Invoke()
        {
            string filePath = ProjectHelpers.ToAbsoluteFilePath(_path, ProjectHelpers.GetActiveFile());
            ApplyChanges(filePath);
        }

        private void ApplyChanges(string filePath)
        {
            ITextSnapshot snapshot = _span.TextBuffer.CurrentSnapshot;

            if (File.Exists(filePath))
            {
                string dataUri = "url('" + FileHelpers.ConvertToBase64(filePath) + "')";
                InsertEmbedString(snapshot, dataUri);
            }
            else
            {
                const string title = "Web Essentials: File not found";
                string message = String.Format("'{0}' could not be resolved.", _path);

                if (WESettings.GetBoolean(WESettings.Keys.AllMessagesToOutputWindow))
                {
                    Logger.Log(String.Format("{0}: {1}", title, message));
                }
                else
                {
                    MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void InsertEmbedString(ITextSnapshot snapshot, string dataUri)
        {
            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            Declaration dec = _url.FindType<Declaration>();

            _span.TextBuffer.Replace(_span.GetSpan(snapshot), dataUri);

            EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
            EditorExtensionsPackage.ExecuteCommand("Edit.CollapsetoDefinitions");
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
