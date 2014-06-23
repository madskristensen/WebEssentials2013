using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class VendorSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private Declaration _declaration;
        private IEnumerable<string> _prefixes;

        public VendorSmartTagAction(ITrackingSpan span, Declaration declaration, IEnumerable<string> prefixes)
        {
            _span = span;
            _declaration = declaration;
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
            RuleBlock rule = _declaration.FindType<RuleBlock>();
            StringBuilder sb = new StringBuilder();
            string separator = rule.Text.Contains("\r") || rule.Text.Contains("\n") ? Environment.NewLine : " ";

            foreach (var entry in _prefixes)
            {
                sb.Append(entry + _declaration.Text + separator);
            }

            using (WebEssentialsPackage.UndoContext((DisplayText)))
            {
                _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), sb.ToString() + _declaration.Text);
                if (separator == Environment.NewLine)
                {
                    WebEssentialsPackage.ExecuteCommand("Edit.FormatSelection");
                }
            }
        }
    }
}
