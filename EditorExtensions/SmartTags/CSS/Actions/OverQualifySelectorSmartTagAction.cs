using System;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class OverQualifySelectorSmartTagAction : CssSmartTagActionBase
    {
        private Selector _selector;
        private ITrackingSpan _span;
        private int _index;

        public OverQualifySelectorSmartTagAction(Selector sel, ITrackingSpan span, int index)
        {
            _selector = sel;
            _span = span;
            _index = index;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/warning.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.OverQualifiedSmartTagActionName; }
        }

        public override void Invoke()
        {
            Span ruleSpan = new Span(_selector.Start, _index);

            using (EditorExtensionsPackage.UndoContext((DisplayText)))
                _span.TextBuffer.Delete(ruleSpan);
        }
    }
}