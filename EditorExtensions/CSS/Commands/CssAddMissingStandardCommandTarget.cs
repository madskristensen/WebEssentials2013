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

namespace MadsKristensen.EditorExtensions.Css
{
    internal class CssAddMissingStandard : CommandTargetBase<CssCommandId>
    {
        private SnapshotPoint? _point;

        public CssAddMissingStandard(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, CssCommandId.AddMissingStandard)
        { }

        protected override bool Execute(CssCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_point == null)
                return false;

            ITextBuffer buffer = _point.Value.Snapshot.TextBuffer;
            CssEditorDocument doc = CssEditorDocument.FromTextBuffer(buffer);
            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
            StringBuilder sb = new StringBuilder(buffer.CurrentSnapshot.GetText());
            int scrollPosition = TextView.TextViewLines.FirstVisibleLine.Extent.Start.Position;

            using (WebEssentialsPackage.UndoContext("Add Missing Standard Property"))
            {
                int count;
                bool hasChanged = AddMissingStandardDeclaration(sb, doc, rootSchema, out count);

                if (hasChanged)
                    buffer.SetText(sb.ToString()
                                     .Replace("/* BEGIN EXTERNAL SOURCE */\r\n", string.Empty)
                                     .Replace("\r\n/* END EXTERNAL SOURCE */\r\n", string.Empty));

                if (TextView.Caret.Position.BufferPosition.Snapshot == buffer.CurrentSnapshot)
                    (WebEssentialsPackage.DTE.ActiveDocument.Selection as TextSelection).GotoLine(1);

                WebEssentialsPackage.ExecuteCommand("Edit.FormatDocument");
                TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, TextView.TextSnapshot.GetLineNumberFromPosition(scrollPosition));
                WebEssentialsPackage.DTE.StatusBar.Text = count + " missing standard properties added";
            }

            return true;
        }

        private static bool AddMissingStandardDeclaration(StringBuilder sb, CssEditorDocument doc, ICssSchemaInstance rootSchema, out int count)
        {
            bool hasChanged = false;
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

                        hasChanged = true;
                    }
                }
            }

            return hasChanged;
        }

        protected override bool IsEnabled()
        {
            _point = TextView.GetSelection("css");

            return _point.HasValue;
        }
    }
}
