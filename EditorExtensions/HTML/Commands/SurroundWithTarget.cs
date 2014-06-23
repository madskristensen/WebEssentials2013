using System;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Html
{
    internal class SurroundWith : CommandTargetBase<FormattingCommandId>
    {
        private IWpfTextView _view;
        private ITextBuffer _buffer;

        public SurroundWith(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, FormattingCommandId.SurroundWith)
        {
            _view = textView;
            _buffer = textView.TextBuffer;
        }

        protected override bool Execute(FormattingCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_view.Selection.IsEmpty)
            {
                return HandleElement();
            }
            else
            {
                int start = _view.Selection.Start.Position.Position;
                int end = _view.Selection.End.Position.Position;
                Update(start, end);
                return true;
            }
        }

        private bool HandleElement()
        {
            HtmlEditorDocument document = HtmlEditorDocument.FromTextView(_view);
            var tree = document.HtmlEditorTree;

            int position = _view.Caret.Position.BufferPosition.Position;

            ElementNode tag = null;
            AttributeNode attr = null;

            tree.GetPositionElement(position, out tag, out attr);

            if (tag != null && (tag.EndTag != null || tag.IsSelfClosing()))
            {
                int start = tag.Start;
                int end = tag.End;

                Update(start, end);
                return true;
            }

            return false;
        }

        private void Update(int start, int end)
        {
            using (WebEssentialsPackage.UndoContext("Surround with..."))
            {
                using (var edit = _buffer.CreateEdit())
                {
                    edit.Insert(end, "</div>");
                    edit.Insert(start, "<div>");
                    edit.Apply();
                }

                SnapshotPoint point = new SnapshotPoint(_buffer.CurrentSnapshot, start + 1);

                _view.Caret.MoveTo(point);
                _view.Selection.Select(new SnapshotSpan(_buffer.CurrentSnapshot, point, 3), false);
                WebEssentialsPackage.ExecuteCommand("Edit.FormatSelection");
            }
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}