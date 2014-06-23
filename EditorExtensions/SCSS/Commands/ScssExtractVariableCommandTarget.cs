using System;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Scss.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Scss
{
    internal class ScssExtractVariableCommandTarget : CommandTargetBase<ExtractCommandId>
    {

        public ScssExtractVariableCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, ExtractCommandId.ExtractVariable)
        {
        }

        protected override bool Execute(ExtractCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("SCSS");

            if (point == null)
                return false;

            ITextBuffer buffer = point.Value.Snapshot.TextBuffer;
            var doc = CssEditorDocument.FromTextBuffer(buffer);
            ParseItem item = doc.StyleSheet.ItemBeforePosition(point.Value);
            ParseItem rule = FindParent(item);

            string text = item.Text;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Name of the variable", "Web Essentials");

            if (string.IsNullOrEmpty(name))
                return false;

            using (WebEssentialsPackage.UndoContext(("Extract to variable")))
            {
                buffer.Insert(rule.Start, "$" + name + ": " + text + ";" + Environment.NewLine + Environment.NewLine);

                Span span = TextView.Selection.SelectedSpans[0].Span;
                TextView.TextBuffer.Replace(span, "$" + name);
            }

            return true;
        }

        public static ParseItem FindParent(ParseItem item)
        {
            ParseItem parent = item.Parent;

            while (true)
            {
                if (parent.Parent == null || parent.Parent is ScssStyleSheet || parent.Parent is AtDirective)
                    break;

                parent = parent.Parent;
            }

            return parent;
        }

        protected override bool IsEnabled()
        {
            var span = TextView.Selection.SelectedSpans[0];
            return span.Length > 0 && !span.GetText().Contains("\n") && TextView.GetSelection("SCSS") != null;
        }
    }
}