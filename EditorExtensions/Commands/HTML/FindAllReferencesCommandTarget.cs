using System;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class HtmlFindAllReferences : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        private readonly HtmlEditorTree _tree;
        private string _className;

        public HtmlFindAllReferences(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd97CmdID.FindReferences)
        {
            _tree = HtmlEditorDocument.FromTextView(textView).HtmlEditorTree;
        }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!string.IsNullOrEmpty(_className))
            {
                FileHelpers.SearchFiles("." + _className, "*.css;*.less;*.scss;*.sass");
            }

            return true;
        }

        private bool TryGetClassName(out string className)
        {
            int position = TextView.Caret.Position.BufferPosition.Position;
            className = null;

            ElementNode element = null;
            AttributeNode attr = null;

            _tree.GetPositionElement(position, out element, out attr);

            if (attr == null || attr.Name != "class")
                return false;

            int beginning = position - attr.ValueRangeUnquoted.Start;
            int start = attr.Value.LastIndexOf(' ', beginning) + 1;
            int length = attr.Value.IndexOf(' ', start) - start;

            if (length < 0)
                length = attr.ValueRangeUnquoted.Length - start;

            className = attr.Value.Substring(start, length);

            return true;
        }

        protected override bool IsEnabled()
        {
            return TryGetClassName(out _className);
        }
    }
}