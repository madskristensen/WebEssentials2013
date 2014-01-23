using System;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class ExpandSelection : CommandTargetBase<FormattingCommandId>
    {
        private IWpfTextView _view;
        private ITextBuffer _buffer;

        public ExpandSelection(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, FormattingCommandId.ExpandSelection)
        {
            _view = textView;
            _buffer = textView.TextBuffer;
        }

        protected override bool Execute(FormattingCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            HtmlEditorDocument document = HtmlEditorDocument.FromTextView(_view);
            var tree = document.HtmlEditorTree;

            int start = _view.Selection.Start.Position.Position;
            int end = _view.Selection.End.Position.Position;

            ElementNode tag = null;
            AttributeNode attr = null;

            tree.GetPositionElement(start, out tag, out attr);

            if (tag == null)
                return false;

            if (tag.EndTag != null && tag.StartTag.End == start && tag.EndTag.Start == end)
            {
                Select(tag.Start, tag.OuterRange.Length);
            }
            else if (tag.Children.Count > 1 && tag.Children[0].Start == start && tag.Children.Last().End == end)
            {
                Select(tag.InnerRange.Start, tag.InnerRange.Length);
            }
            else if (tag.EndTag != null && tag.Children.Count > 1 && tag.StartTag.Start < start && tag.EndTag.End > end)
            {
                Select(tag.Children[0].Start, tag.Children.Last().End - tag.Children[0].Start);
            }
            else if (tag.EndTag != null && tag.StartTag.Start < start && tag.EndTag.End > end)
            {
                Select(tag.InnerRange.Start, tag.InnerRange.Length);
            }
            else if (tag.IsSelfClosing() && tag.Start < start && tag.End > end)
            {
                Select(tag.Start, tag.OuterRange.Length);
            }
            else if (tag.Parent != null)
            {
                Select(tag.Parent.Start, tag.Parent.OuterRange.Length);
            }

            return true;
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