using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace MadsKristensen.EditorExtensions
{
    internal class ContactSelection : CommandTargetBase
    {
        private IWpfTextView _view;
        private ITextBuffer _buffer;

        public ContactSelection(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidFormattingCmdSet, PkgCmdIDList.ContractSelection)
        {
            _view = textView;
            _buffer = textView.TextBuffer;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            HtmlEditorDocument document = HtmlEditorDocument.FromTextView(_view);
            var tree = document.HtmlEditorTree;

            int start = _view.Selection.Start.Position.Position;
            int end = _view.Selection.End.Position.Position;

            ElementNode tag = null;
            AttributeNode attr = null;

            tree.GetPositionElement(start + 1, out tag, out attr);

            if (tag == null)
                return false;

            if (tag.EndTag != null && tag.StartTag.Start == start && tag.EndTag.End == end)
            {
                Select(tag.InnerRange.Start, tag.InnerRange.Length);
            }
            else if (tag.Children.Count > 0)
            {
                var current = NodeAtCaret(tree);
                var child = ChildNode(current, tag);

                if (tag.Children.Contains(child))
                    Select(child.Start, child.OuterRange.Length);
                else
                    Select(tag.Children[0].Start, tag.Children[0].End - tag.Children[0].Start);
            }

            return true;
        }

        private ElementNode ChildNode(ElementNode deepChild, ElementNode parent)
        {
            if (deepChild.Parent != parent)
            {
                return ChildNode(deepChild.Parent, parent);
            }

            return deepChild;
        }

        private ElementNode NodeAtCaret(HtmlEditorTree tree)
        {
            int start = _view.Caret.Position.BufferPosition.Position;

            ElementNode tag = null;
            AttributeNode attr = null;

            tree.GetPositionElement(start, out tag, out attr);

            return tag;
        }

        private void Select(int start, int length)
        {
            var span = new SnapshotSpan(_buffer.CurrentSnapshot, start, length);
            _view.Selection.Select(span, false);
        }

        protected override bool IsEnabled()
        {
            return true;
        }

    }
}