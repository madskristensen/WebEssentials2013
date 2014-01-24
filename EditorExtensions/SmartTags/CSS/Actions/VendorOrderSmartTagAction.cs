using System;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class VendorOrderSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private Declaration _lastVendor;
        private Declaration _standard;

        public VendorOrderSmartTagAction(ITrackingSpan span, Declaration lastVendor, Declaration standard)
        {
            _span = span;
            _lastVendor = lastVendor;
            _standard = standard;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/warning.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.VendorOrderSmartTagActionName; }
        }

        public override void Invoke()
        {
            string separator = Microsoft.CSS.Editor.CssSettings.FormatterBlockBracePosition == BracePosition.Compact ? " " : Environment.NewLine;
            string insert = _lastVendor.Text + separator + _standard.Text;

            using (EditorExtensionsPackage.UndoContext((DisplayText)))
            {
                _span.TextBuffer.Replace(new Span(_lastVendor.Start, _lastVendor.Length), insert);
                _span.TextBuffer.Delete(new Span(_standard.Start, _standard.Length));
                EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
            }
        }
    }
}
