using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class UpdateEmbedSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private string _path;

        public UpdateEmbedSmartTagAction(ITrackingSpan span, string path)
        {
            _span = span;
            _path = path;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/embed.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return string.Format(CultureInfo.CurrentCulture, Resources.UpdateEmbedSmartTagActionName, _path); }
        }

        public async override void Invoke()
        {
            string filePath = ProjectHelpers.ToAbsoluteFilePath(_path, _span.TextBuffer.GetFileName());
            await ApplyChanges(filePath);
        }

        private async Task ApplyChanges(string filePath)
        {
            ITextSnapshot snapshot = _span.TextBuffer.CurrentSnapshot;

            if (File.Exists(filePath))
            {
                string dataUri = "url('" + await FileHelpers.ConvertToBase64(filePath) + "')";
                InsertEmbedString(snapshot, dataUri);
            }
            else
            {
                Logger.ShowMessage(String.Format(CultureInfo.CurrentCulture, "'{0}' could not be resolved.", _path), "Web Essentials: File not found");
            }
        }

        private void InsertEmbedString(ITextSnapshot snapshot, string dataUri)
        {
            using (WebEssentialsPackage.UndoContext((DisplayText)))
            {
                _span.TextBuffer.Replace(_span.GetSpan(snapshot), dataUri);
                WebEssentialsPackage.ExecuteCommand("Edit.FormatSelection");
                WebEssentialsPackage.ExecuteCommand("Edit.CollapsetoDefinitions");
            }
        }
    }
}