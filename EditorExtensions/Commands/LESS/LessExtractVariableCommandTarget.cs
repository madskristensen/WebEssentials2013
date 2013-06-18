using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace MadsKristensen.EditorExtensions
{
    internal class LessExtractVariableCommandTarget : CommandTargetBase
    {
        private DTE2 _dte;

        public LessExtractVariableCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidExtractCmdSet, PkgCmdIDList.ExtractVariable)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (TextView == null)
                return false;

            CssEditorDocument document = CssEditorDocument.FromTextBuffer(TextView.TextBuffer);

            int position = TextView.Caret.Position.BufferPosition.Position;
            ParseItem item = document.Tree.StyleSheet.ItemBeforePosition(position);

            ParseItem rule = FindParent(item);
            string text = item.Text;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Name of the variable", "Web Essentials");

            if (!string.IsNullOrEmpty(name))
            {
                EditorExtensionsPackage.DTE.UndoContext.Open("Extract to variable");

                Span span = TextView.Selection.SelectedSpans[0].Span;
                TextView.TextBuffer.Replace(span, "@" + name);
                TextView.TextBuffer.Insert(rule.Start, "@" + name + ": " + text + ";" + Environment.NewLine + Environment.NewLine);

                EditorExtensionsPackage.DTE.UndoContext.Close();

                return true;
            }

            return false;
        }

        public static ParseItem FindParent(ParseItem item)
        {
            ParseItem parent = item.Parent;

            while (true)
            {
                if (parent.Parent == null || parent.Parent is LessStyleSheet || parent.Parent is AtDirective)
                    break;

                parent = parent.Parent;
            }

            return parent;
        }

        protected override bool IsEnabled()
        {
            var span = TextView.Selection.SelectedSpans[0];
            return span.Length > 0 && !span.GetText().Contains("\r");
        }
    }
}