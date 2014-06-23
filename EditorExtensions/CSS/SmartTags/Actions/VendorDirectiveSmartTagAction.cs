using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class VendorDirectiveSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private AtDirective _directive;
        private IEnumerable<string> _prefixes;

        public VendorDirectiveSmartTagAction(ITrackingSpan span, AtDirective directive, IEnumerable<string> prefixes)
        {
            _span = span;
            _directive = directive;
            _prefixes = prefixes;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/warning.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.VendorSmartTagActionName; }
        }

        public override void Invoke()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var entry in _prefixes)
            {
                string text = _directive.Text.Replace("@" + _directive.Keyword.Text, entry);
                sb.Append(text + Environment.NewLine + Environment.NewLine);
            }

            using (WebEssentialsPackage.UndoContext((DisplayText)))
                _span.TextBuffer.Replace(new Span(_directive.Start, _directive.Length), sb.ToString() + _directive.Text);
        }
    }
}
