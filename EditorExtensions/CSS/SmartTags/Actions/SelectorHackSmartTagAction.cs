using System;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class SelectorHackSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private Selector _selector;
        private string _hack;
        private string _displayText;

        public SelectorHackSmartTagAction(ITrackingSpan span, Selector url, string hackPrefix, string displayText)
        {
            _span = span;
            _selector = url;
            _hack = hackPrefix;
            _displayText = displayText;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/skull.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return this._displayText; }
        }

        public override void Invoke()
        {
            using (WebEssentialsPackage.UndoContext((DisplayText)))
                _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), _hack + _selector.Text);
        }
    }
}
