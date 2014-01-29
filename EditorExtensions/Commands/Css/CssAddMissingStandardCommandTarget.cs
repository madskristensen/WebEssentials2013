using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class CssAddMissingStandard : CommandTargetBase<CssCommandId>
    {

        public CssAddMissingStandard(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, CssCommandId.AddMissingStandard)
        {
        }

        protected override bool Execute(CssCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("css");
            if (point == null) return false;

            ITextBuffer buffer = point.Value.Snapshot.TextBuffer;
            CssEditorDocument doc = CssEditorDocument.FromTextBuffer(buffer);
            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);

            StringBuilder sb = new StringBuilder(buffer.CurrentSnapshot.GetText());
            int scrollPosition = TextView.TextViewLines.FirstVisibleLine.Extent.Start.Position;

            using (EditorExtensionsPackage.UndoContext("Add Missing Standard Property"))
            {
                int count;
                string result = AddMissingStandardDeclaration(sb, doc, rootSchema, out count);
                Span span = new Span(0, buffer.CurrentSnapshot.Length);
                buffer.Replace(span, result);

                var selection = EditorExtensionsPackage.DTE.ActiveDocument.Selection as TextSelection;
                selection.GotoLine(1);

                EditorExtensionsPackage.ExecuteCommand("Edit.FormatDocument");
                TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, TextView.TextSnapshot.GetLineNumberFromPosition(scrollPosition));
                EditorExtensionsPackage.DTE.StatusBar.Text = count + " missing standard properties added";
            }

            return true;
        }

        private static string AddMissingStandardDeclaration(StringBuilder sb, CssEditorDocument doc, ICssSchemaInstance rootSchema, out int count)
        {
            var visitor = new CssItemCollector<RuleBlock>(true);
            doc.Tree.StyleSheet.Accept(visitor);
            count = 0;

            //var items = visitor.Items.Where(d => d.IsValid && d.IsVendorSpecific());
            foreach (RuleBlock rule in visitor.Items.Reverse())
            {
                HashSet<string> list = new HashSet<string>();
                foreach (Declaration dec in rule.Declarations.Where(d => d.IsValid && d.IsVendorSpecific()).Reverse())
                {
                    ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, dec);
                    ICssCompletionListEntry entry = VendorHelpers.GetMatchingStandardEntry(dec, schema);

                    if (entry != null && !list.Contains(entry.DisplayText) && !rule.Declarations.Any(d => d.PropertyName != null && d.PropertyName.Text == entry.DisplayText))
                    {
                        int index = dec.Text.IndexOf(":", StringComparison.Ordinal);
                        string standard = entry.DisplayText + dec.Text.Substring(index);

                        sb.Insert(dec.AfterEnd, standard);
                        list.Add(entry.DisplayText);
                        count++;
                    }
                }
            }

            return sb.ToString();
        }

        protected override bool IsEnabled()
        {
            return TextView.GetSelection("css").HasValue;
        }
    }
}