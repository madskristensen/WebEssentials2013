using System;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class LessExtractMixinCommandTarget : CommandTargetBase
    {
        private DTE2 _dte;

        public LessExtractMixinCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidExtractCmdSet, PkgCmdIDList.ExtractMixin)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
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
                EditorExtensionsPackage.DTE.UndoContext.Open("Extract to mixin");

                string text = TextView.Selection.SelectedSpans[0].GetText();
                buffer.Insert(rule.Start, "." + name + "() {" + Environment.NewLine + text + Environment.NewLine + "}" + Environment.NewLine + Environment.NewLine);

                var selection = TextView.Selection.SelectedSpans[0];
                TextView.TextBuffer.Replace(selection.Span, "." + name + "();");

                TextView.Selection.Select(new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, mixinStart, 1), false);
                EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
                TextView.Selection.Clear();

                EditorExtensionsPackage.DTE.UndoContext.Close();

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