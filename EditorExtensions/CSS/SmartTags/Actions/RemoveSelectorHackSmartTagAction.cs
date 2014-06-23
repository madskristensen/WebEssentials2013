using System;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class RemoveSelectorHackSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private Selector _selector;
        private string _hack;

        public RemoveSelectorHackSmartTagAction(ITrackingSpan span, Selector url, string hackPrefix)
        {
            _span = span;
            _selector = url;
            _hack = hackPrefix;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/no_skull.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.RemoveSelectorHackSmartTagActionName; }
        }

        public override void Invoke()
        {
            using (WebEssentialsPackage.UndoContext((DisplayText)))
                _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), _selector.Text.Substring(_hack.Length));
        }
    }
}
