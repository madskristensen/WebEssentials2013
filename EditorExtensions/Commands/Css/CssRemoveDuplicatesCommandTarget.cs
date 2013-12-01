using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class CssRemoveDuplicates : CommandTargetBase
    {

        public CssRemoveDuplicates(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidCssCmdSet, PkgCmdIDList.cssRemoveDuplicates)
        {
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("CSS");
            if (point == null)
                return false;
            ITextSnapshot snapshot = point.Value.Snapshot;
            var doc = CssEditorDocument.FromTextBuffer(snapshot.TextBuffer);

            StringBuilder sb = new StringBuilder(snapshot.GetText());

            using (EditorExtensionsPackage.UndoContext("Remove Duplicate Properties"))
            {
                string result = RemoveDuplicateProperties(sb, doc);
                Span span = new Span(0, snapshot.Length);
                snapshot.TextBuffer.Replace(span, result);

                var selection = EditorExtensionsPackage.DTE.ActiveDocument.Selection as TextSelection;
                selection.GotoLine(1);

                EditorExtensionsPackage.ExecuteCommand("Edit.FormatDocument");
            }

            return true;
        }

        private static string RemoveDuplicateProperties(StringBuilder sb, CssEditorDocument doc)
        {
            var visitor = new CssItemCollector<RuleBlock>(true);
            doc.Tree.StyleSheet.Accept(visitor);

            foreach (RuleBlock rule in visitor.Items.Reverse())
            {
                HashSet<string> list = new HashSet<string>();

                foreach (Declaration dec in rule.Declarations.Reverse())
                {
                    if (!list.Add(dec.Text))
                        sb.Remove(dec.Start, dec.Length);
                }
            }

            return sb.ToString();
        }

        protected override bool IsEnabled()
        {
            return TextView.GetSelection("CSS") != null;
        }
    }
}