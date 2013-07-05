//using Microsoft.Html.Core;
//using Microsoft.Html.Editor;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.TextManager.Interop;
//using System;

//namespace MadsKristensen.EditorExtensions
//{
//    internal class EnterFormat : CommandTargetBase
//    {
//        private HtmlEditorTree _tree;

//        public EnterFormat(IVsTextView adapter, IWpfTextView textView)
//            : base(adapter, textView, typeof(Microsoft.VisualStudio.VSConstants.VSStd2KCmdID).GUID, 3)
//        {
//            _tree = HtmlEditorDocument.FromTextView(textView).HtmlEditorTree;
//        }

//        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
//        {
//            int position = TextView.Caret.Position.BufferPosition.Position;
//            var point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, position);
//            var line = TextView.GetTextViewLineContainingBufferPosition(point);

//            ElementNode element = null;
//            AttributeNode attr = null;

//            _tree.GetPositionElement(position, out element, out attr);

//            if (element == null || element.Name == "body" || position != element.StartTag.End || line.End.Position == position)
//                return false;

//            UpdateTextBuffer(element, position);

//            return true;
//        }

//        private void UpdateTextBuffer(ElementNode element, int position)
//        {
//            EditorExtensionsPackage.DTE.UndoContext.Open("Format on enter");

//            TextView.TextBuffer.Insert(position, Environment.NewLine);

//            FormatTag(element);
//            PlaceCaret(element, position);

//            EditorExtensionsPackage.DTE.UndoContext.Close();
//        }

//        private void FormatTag(ElementNode element)
//        {
//            // HACK: Use the RangeFormatter instead and format the first parent block element 
//            element = element.Parent ?? element;

//            var span = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, element.Start, element.Length);
//            TextView.Selection.Select(span, false);

//            EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");

//            TextView.Selection.Clear();
//        }

//        private void PlaceCaret(ElementNode element, int position)
//        {
//            string text = TextView.TextBuffer.CurrentSnapshot.GetText(element.InnerRange.Start, element.InnerRange.Length);

//            for (int i = 0; i < text.Length; i++)
//            {
//                if (!char.IsWhiteSpace(text[i]))
//                {
//                    var firstChild = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, element.InnerRange.Start + i);
//                    TextView.Caret.MoveTo(firstChild);
//                    break;
//                }
//            }
//        }

//        protected override bool IsEnabled()
//        {
//            return true;
//        }
//    }
//}