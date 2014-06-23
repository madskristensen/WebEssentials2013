using System;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Less
{
    internal class LessExtractMixinCommandTarget : CommandTargetBase<ExtractCommandId>
    {
        public LessExtractMixinCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, ExtractCommandId.ExtractMixin)
        {
        }

        protected override bool Execute(ExtractCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("LESS");

            if (point == null)
                return false;

            var buffer = point.Value.Snapshot.TextBuffer;
            var doc = CssEditorDocument.FromTextBuffer(buffer);
            ParseItem item = doc.StyleSheet.ItemBeforePosition(point.Value);

            ParseItem rule = LessExtractVariableCommandTarget.FindParent(item);
            int mixinStart = rule.Start;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Name of the Mixin", "Web Essentials");

            if (!string.IsNullOrEmpty(name))
            {
                using (WebEssentialsPackage.UndoContext(("Extract to mixin")))
                {
                    string text = TextView.Selection.SelectedSpans[0].GetText();
                    buffer.Insert(rule.Start, "." + name + "() {" + Environment.NewLine + text + Environment.NewLine + "}" + Environment.NewLine + Environment.NewLine);

                    var selection = TextView.Selection.SelectedSpans[0];
                    TextView.TextBuffer.Replace(selection.Span, "." + name + "();");

                    TextView.Selection.Select(new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, mixinStart, 1), false);
                    WebEssentialsPackage.ExecuteCommand("Edit.FormatSelection");
                    TextView.Selection.Clear();
                }

                return true;
            }

            return false;
        }

        protected override bool IsEnabled()
        {
            return TextView.Selection.SelectedSpans[0].Length > 0 && TextView.GetSelection("LESS") != null;
        }
    }
}