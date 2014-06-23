using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class CssAddMissingVendor : CommandTargetBase<CssCommandId>
    {
        private SnapshotPoint? _point;

        public CssAddMissingVendor(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, CssCommandId.AddMissingVendor)
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

            using (WebEssentialsPackage.UndoContext("Add Missing Vendor Specifics"))
            {
                int count;
                bool hasChanged = AddMissingVendorDeclarations(sb, doc, rootSchema, out count);

                if (hasChanged)
                    buffer.SetText(sb.ToString()
                                     .Replace("/* BEGIN EXTERNAL SOURCE */\r\n", string.Empty)
                                     .Replace("\r\n/* END EXTERNAL SOURCE */\r\n", string.Empty));

                if (TextView.Caret.Position.BufferPosition.Snapshot == buffer.CurrentSnapshot)
                    (WebEssentialsPackage.DTE.ActiveDocument.Selection as TextSelection).GotoLine(1);

                WebEssentialsPackage.ExecuteCommand("Edit.FormatDocument");
                TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, TextView.TextSnapshot.GetLineNumberFromPosition(scrollPosition));
                WebEssentialsPackage.DTE.StatusBar.Text = count + " missing vendor specific properties added";
            }

            return true;
        }

        private static bool AddMissingVendorDeclarations(StringBuilder sb, CssEditorDocument doc, ICssSchemaInstance rootSchema, out int count)
        {
            bool hasChanged = false;
            var visitor = new CssItemCollector<Declaration>(true);

            doc.Tree.StyleSheet.Accept(visitor);
            count = 0;

            var items = visitor.Items.Where(d => d.IsValid && !d.IsVendorSpecific() && d.PropertyName.Text != "filter");

            foreach (Declaration dec in items.Reverse())
            {
                ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, dec);
                var missingEntries = dec.GetMissingVendorSpecifics(schema);

                if (missingEntries.Any())
                {
                    var missingPrefixes = missingEntries.Select(e => e.Substring(0, e.IndexOf('-', 1) + 1));
                    string vendors = GetVendorDeclarations(missingPrefixes, dec);

                    sb.Insert(dec.Start, vendors);
                    count++;

                    hasChanged = true;
                }
            }

            return hasChanged;
        }

        private static string GetVendorDeclarations(IEnumerable<string> prefixes, Declaration declaration)
        {
            StringBuilder sb = new StringBuilder();
            string separator = true ? Environment.NewLine : " ";

            foreach (var entry in prefixes)
            {
                sb.Append(entry + declaration.Text + separator);
            }

            return sb.ToString();
        }

        protected override bool IsEnabled()
        {
            _point = TextView.GetSelection("css");

            return _point.HasValue;
        }
    }
}
