using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.Html.Schemas;
using Microsoft.Html.Schemas.Model;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor.Formatting;

namespace MadsKristensen.EditorExtensions.Html
{
    internal class EnterFormat : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private HtmlEditorTree _tree;
        private IEditorRangeFormatter _formatter;
        private ICompletionBroker _broker;

        public EnterFormat(IVsTextView adapter, IWpfTextView textView, IEditorFormatterProvider formatterProvider, ICompletionBroker broker)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.RETURN)
        {
            HtmlEditorDocument document = HtmlEditorDocument.TryFromTextView(textView);

            _tree = document == null ? null : document.HtmlEditorTree;
            _formatter = formatterProvider.CreateRangeFormatter();
            _broker = broker;
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_tree == null || _broker.IsCompletionActive(TextView) || !IsValidTextBuffer() || !WESettings.Instance.Html.EnableEnterFormat)
                return false;

            int position = TextView.Caret.Position.BufferPosition.Position;
            SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, position);
            IWpfTextViewLine line = TextView.GetTextViewLineContainingBufferPosition(point);

            ElementNode element = null;
            AttributeNode attr = null;

            _tree.GetPositionElement(position, out element, out attr);

            if (element == null ||
                _tree.IsDirty ||
                element.Parent == null ||
                element.StartTag.Contains(position) ||
                line.End.Position == position || // caret at end of line (TODO: add ignore whitespace logic)
                TextView.TextBuffer.CurrentSnapshot.GetText(element.InnerRange.Start, element.InnerRange.Length).Trim().Length == 0)
                return false;

            UpdateTextBuffer(element);

            return false;
        }

        private bool IsValidTextBuffer()
        {
            if (TextView.TextBuffer.ContentType.IsOfType("markdown"))
                return false;
            var projection = TextView.TextBuffer as IProjectionBuffer;

            if (projection != null)
            {
                var snapshotPoint = TextView.Caret.Position.BufferPosition;

                var buffers = projection.SourceBuffers.Where(
                    s =>
                        !s.ContentType.IsOfType("html")
                        && !s.ContentType.IsOfType("htmlx")
                        && !s.ContentType.IsOfType("inert")
                        && !s.ContentType.IsOfType("CSharp")
                        && !s.ContentType.IsOfType("VisualBasic")
                        && !s.ContentType.IsOfType("RoslynCSharp")
                        && !s.ContentType.IsOfType("RoslynVisualBasic"));


                foreach (ITextBuffer buffer in buffers)
                {
                    SnapshotPoint? point = TextView.BufferGraph.MapDownToBuffer(snapshotPoint, PointTrackingMode.Negative, buffer, PositionAffinity.Predecessor);

                    if (point.HasValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void UpdateTextBuffer(ElementNode element)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                FormatTag(element);
                PlaceCaret(element);
            }), DispatcherPriority.Normal, null);
        }

        private void FormatTag(ElementNode element)
        {
            try
            {
                var schemas = GetSchemas();

                element = GetFirstBlockParent(element, schemas);

                HtmlEditorDocument document = HtmlEditorDocument.TryFromTextView(TextView);

                if (document == null)
                    return;

                ITextBuffer buffer = document.TextBuffer;
                SnapshotSpan span = new SnapshotSpan(buffer.CurrentSnapshot, element.Start, element.Length);

                _formatter.FormatRange(TextView, buffer, span, true);
            }
            catch
            { }
        }

        public static List<IHtmlSchema> GetSchemas()
        {
            HtmlSchemaManager mng = new HtmlSchemaManager();
            IHtmlSchema html = mng.GetSchema("http://schemas.microsoft.com/intellisense/html");

            var schemas = mng.CustomAttributePrefixes.SelectMany(p => mng.GetSupplementalSchemas(p)).ToList();
            schemas.Insert(0, html);

            return schemas;
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

        private void PlaceCaret(ElementNode element)
        {
            SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, TextView.Caret.Position.BufferPosition.Position);

            if (element.EndTag == null)
                return;

            if (element.EndTag.Start == point.Position)
            {
                string text = element.GetText(element.InnerRange);

                for (int i = text.Length - 1; i > -1; i--)
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