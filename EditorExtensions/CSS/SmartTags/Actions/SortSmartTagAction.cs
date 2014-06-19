//using System;
//using System.Windows.Media.Imaging;
//using CssSorter;
//using Microsoft.CSS.Core;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Editor;

//namespace MadsKristensen.EditorExtensions.Css
//{
//    internal class SortSmartTagAction : CssSmartTagActionBase
//    {
//        private RuleSet _rule;
//        private ITrackingSpan _span;
//        private ITextView _view;

//        public SortSmartTagAction(RuleSet rule, ITrackingSpan span, ITextView view)
//        {
//            _rule = rule;
//            _span = span;
//            _view = view;

//            if (Icon == null)
//            {
//                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/sort.png", UriKind.RelativeOrAbsolute));
//            }
//        }

//        public override string DisplayText
//        {
//            get { return "Sort Properties"; }
//        }

//        public override void Invoke()
//        {
//            Span ruleSpan = new Span(_rule.Start, _rule.Length);

//            Sorter sorter = new Sorter();
//            string result = null;

//            if (_view.TextBuffer.ContentType.IsOfType("LESS"))
//            {
//                result = sorter.SortLess(_rule.Text);
//            }
//            else
//            {
//                result = sorter.SortStyleSheet(_rule.Text);
//            }
//            var position = _view.Caret.Position.BufferPosition;

//            using (EditorExtensionsPackage.UndoContext((DisplayText)))
//            {
//                _span.TextBuffer.Replace(ruleSpan, result);
//                _view.Caret.MoveTo(new SnapshotPoint(position.Snapshot.TextBuffer.CurrentSnapshot, position));
//                EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
//            }
//        }
//    }


//    //public class DeclarationComparer : IComparer<ParseItem>
//    //{
//    //    public int Compare(ParseItem x, ParseItem y)
//    //    {
//    //        if (x == null || y == null)
//    //            return 0;

//    //        if (!x.Text.StartsWith("-") && !x.Text.StartsWith("*") && !x.Text.StartsWith("_") &&
//    //            !y.Text.StartsWith("-") && !y.Text.StartsWith("*") && !y.Text.StartsWith("_"))
//    //        {
//    //            // Replace hyphens because of vendor specific values such as -ms-linear-gradient()
//    //            return x.Text.CompareTo(y.Text);
//    //        }            

//    //        string xText = GetStandardName(x.Text);
//    //        string yText = GetStandardName(y.Text);

//    //        if (x.Text.StartsWith("-") && y.Text.StartsWith("-"))
//    //        {
//    //            return xText.CompareTo(yText);
//    //        }

//    //        if (x.Text.StartsWith("-") && !y.Text.StartsWith("-"))
//    //        {
//    //            if (xText == yText)
//    //                return -1;
//    //        }

//    //        if (!x.Text.StartsWith("-") && y.Text.StartsWith("-"))
//    //        {
//    //            if (xText == yText)
//    //                return 1;
//    //        }

//    //        return xText.CompareTo(yText);
//    //    }

//    //    public string GetStandardName(string name)
//    //    {
//    //        name = name.Trim('*', '_');
//    //        if (name.Length > 0 && name[0] == '-')
//    //        {
//    //            int index = name.IndexOf('-', 1) + 1;
//    //            name = index > -1 ? name.Substring(index) : name;
//    //        }

//    //        return name;
//    //    }
//    //}
//}
