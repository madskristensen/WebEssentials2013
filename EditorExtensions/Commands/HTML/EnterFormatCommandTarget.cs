using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.Html.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor.Formatting;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    internal class EnterFormat : CommandTargetBase
    {
        private HtmlEditorTree _tree;
        private IEditorRangeFormatter _formatter;

        public EnterFormat(IVsTextView adapter, IWpfTextView textView, IEditorFormatterProvider formatterProvider)
            : base(adapter, textView, typeof(Microsoft.VisualStudio.VSConstants.VSStd2KCmdID).GUID, 3)
        {
            _tree = HtmlEditorDocument.FromTextView(textView).HtmlEditorTree;
            _formatter = formatterProvider.CreateRangeFormatter();
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int position = TextView.Caret.Position.BufferPosition.Position;
            SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, position);
            IWpfTextViewLine line = TextView.GetTextViewLineContainingBufferPosition(point);

            ElementNode element = null;
            AttributeNode attr = null;

            _tree.GetPositionElement(position, out element, out attr);

            if (element == null ||
                _tree.IsDirty ||
                line.End.Position == position || // caret at end of line (TODO: add ignore whitespace logic)
                TextView.TextBuffer.CurrentSnapshot.GetText(element.InnerRange.Start, element.InnerRange.Length).Trim().Length == 0)
                return false;

            UpdateTextBuffer(element, position);

            return false;
        }

        private void UpdateTextBuffer(ElementNode element, int position)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                FormatTag(element);
                PlaceCaret(element, position);

            }), DispatcherPriority.Normal, null);
        }

        private void FormatTag(ElementNode element)
        {
            var schemas = AttributeNameCompletionProvider.GetSchemas();

            element = GetFirstBlockParent(element, schemas);

            ITextBuffer buffer = HtmlEditorDocument.FromTextView(TextView).TextBuffer;
            SnapshotSpan span = new SnapshotSpan(buffer.CurrentSnapshot, element.Start, element.Length);

            _formatter.FormatRange(TextView, buffer, span, true);
        }

        private ElementNode GetFirstBlockParent(ElementNode current, List<IHtmlSchema> schemas)
        {
            foreach (var schema in schemas)
            {
                IHtmlElementInfo element = schema.GetElementInfo(current.Name);

                if (element != null && element.IsPropertyValueEqual(ElementInfoProperty.Block, "true", true))
                    return current;
            }

            if (current.Parent != null)
                return GetFirstBlockParent(current.Parent, schemas);

            return current;
        }

        private void PlaceCaret(ElementNode element, int position)
        {
            SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, TextView.Caret.Position.BufferPosition.Position);
            
            if (element.EndTag.Start == point.Position)
            {
                string text = element.GetText(element.InnerRange);

                for (int i = text.Length -1; i > -1; i--)
                {
                    if (!char.IsWhiteSpace(text[i]))
                    {
                        TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, element.InnerRange.Start + i + 1));
                        break;
                    }
                }
            }
            else
            {
                IWpfTextViewLine line = TextView.GetTextViewLineContainingBufferPosition(point);
                string text = TextView.TextBuffer.CurrentSnapshot.GetText(line.Start.Position, line.Length);

                for (int i = 0; i < text.Length; i++)
                {
                    if (!char.IsWhiteSpace(text[i]))
                    {
                        TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, line.Start.Position + i));
                        break;
                    }
                }
            }
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}