using System;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Html
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
            HtmlEditorDocument document = HtmlEditorDocument.TryFromTextView(_view);

            if (document == null)
                return false;

            var tree = document.HtmlEditorTree;

            int start = _view.Selection.Start.Position.Position;
            int end = _view.Selection.End.Position.Position;

            ElementNode tag = null;
            AttributeNode attr = null;

            tree.GetPositionElement(start, out tag, out attr);

            if (tag == null)
                return false;

            if (attr != null)
            {
                SelectAttribute(start, end, attr, tag);
            }
            else if (tag.EndTag != null && tag.StartTag.End == start && tag.EndTag.Start == end)
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

        private void SelectAttribute(int start, int end, AttributeNode attr, ElementNode tag)
        {
            if (start >= attr.ValueRange.Start && start <= attr.ValueRange.End &&
               (attr.ValueRange.Start != start || attr.ValueRange.End != end))
            {
                Select(attr.ValueRange.Start, attr.ValueRange.Length);
            }
            else if (attr.Start != start || attr.End != end)
            {
                Select(attr.Start, attr.Length);
            }
            else
            {
                Select(tag.StartTag.Start, tag.StartTag.Length);
            }
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