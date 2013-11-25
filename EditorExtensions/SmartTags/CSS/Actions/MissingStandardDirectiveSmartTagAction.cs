using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class MissingStandardDirectiveSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private AtDirective _directive;
        private string _standardName;
        private ITextView _view;

        public MissingStandardDirectiveSmartTagAction(ITrackingSpan span, AtDirective directive, string standardName, ITextView view)
        {
            _span = span;
            _directive = directive;
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
            //string separator = _directive.Parent.Text.Contains("\r") || _directive.Parent.Text.Contains("\n") ? Environment.NewLine : " ";
            //int index = _directive.Text.IndexOf(":", StringComparison.Ordinal);
            //string newDec = _standardName + _directive.Text.Substring(index);

            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            //SnapshotSpan span = _span.GetSpan(_span.TextBuffer.CurrentSnapshot);
            string text = _directive.Text.Replace("@" + _directive.Keyword.Text, _standardName);
            _span.TextBuffer.Insert(_directive.AfterEnd, Environment.NewLine + Environment.NewLine + text);
            EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
