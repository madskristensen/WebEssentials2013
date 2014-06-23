using System;
using System.Globalization;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class MissingStandardSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private Declaration _declaration;
        private string _standardName;

        public MissingStandardSmartTagAction(ITrackingSpan span, Declaration declaration, string standardName)
        {
            _span = span;
            _declaration = declaration;
            _standardName = standardName;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/warning.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return string.Format(CultureInfo.InvariantCulture, Resources.StandardSmartTagActionName, _standardName); }
        }

        public override void Invoke()
        {
            string separator = _declaration.Parent.Text.Contains("\r") || _declaration.Parent.Text.Contains("\n") ? Environment.NewLine : " ";
            int index = _declaration.Text.IndexOf(":", StringComparison.Ordinal);
            string newDec = _standardName + _declaration.Text.Substring(index);

            using (WebEssentialsPackage.UndoContext((DisplayText)))
            {
                SnapshotSpan span = _span.GetSpan(_span.TextBuffer.CurrentSnapshot);
                _span.TextBuffer.Replace(span, _declaration.Text + separator + newDec);
                WebEssentialsPackage.ExecuteCommand("Edit.FormatSelection");
            }
        }
    }
}
