using System;
using System.Globalization;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class MissingStandardSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private Declaration _declaration;
        private string _standardName;
        private ITextView _view;

        public MissingStandardSmartTagAction(ITrackingSpan span, Declaration declaration, string standardName, ITextView view)
        {
            _span = span;
            _declaration = declaration;
            _standardName = standardName;
            _view = view;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/warning.png", UriKind.RelativeOrAbsolute));
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

            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            SnapshotSpan span = _span.GetSpan(_span.TextBuffer.CurrentSnapshot);
            _span.TextBuffer.Replace(span, _declaration.Text + separator + newDec);
            EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
