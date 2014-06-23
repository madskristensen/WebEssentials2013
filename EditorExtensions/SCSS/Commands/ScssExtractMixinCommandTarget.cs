using System;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Scss
{
    internal class ScssExtractMixinCommandTarget : CommandTargetBase<ExtractCommandId>
    {
        public ScssExtractMixinCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, ExtractCommandId.ExtractMixin)
        {
        }

        protected override bool Execute(ExtractCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("SCSS");

            if (point == null)
                return false;

            var buffer = point.Value.Snapshot.TextBuffer;
            var doc = CssEditorDocument.FromTextBuffer(buffer);
            ParseItem item = doc.StyleSheet.ItemBeforePosition(point.Value);

            ParseItem rule = ScssExtractVariableCommandTarget.FindParent(item);
            int mixinStart = rule.Start;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Name of the Mixin", "Web Essentials");

            if (!string.IsNullOrEmpty(name))
            {
                using (WebEssentialsPackage.UndoContext(("Extract to mixin")))
                {
                    string text = TextView.Selection.SelectedSpans[0].GetText();
                    buffer.Insert(rule.Start, "@mixin " + name + "() {" + Environment.NewLine + text + Environment.NewLine + "}" + Environment.NewLine + Environment.NewLine);

                    var selection = TextView.Selection.SelectedSpans[0];
                    TextView.TextBuffer.Replace(selection.Span, "@include " + name + "();");

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
            return TextView.Selection.SelectedSpans[0].Length > 0 && TextView.GetSelection("SCSS") != null;
        }
    }
}