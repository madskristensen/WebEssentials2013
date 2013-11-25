using EnvDTE;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class CssAddMissingVendor : CommandTargetBase
    {
        public CssAddMissingVendor(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidCssCmdSet, PkgCmdIDList.addMissingVendor)
        {
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("css");
            if (point == null) return false;

            ITextBuffer buffer = point.Value.Snapshot.TextBuffer;
            CssEditorDocument doc = CssEditorDocument.FromTextBuffer(buffer);
            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);

            StringBuilder sb = new StringBuilder(buffer.CurrentSnapshot.GetText());

            EditorExtensionsPackage.DTE.UndoContext.Open("Add Missing Vendor Specifics");

            string result = AddMissingVendorDeclarations(sb, doc, rootSchema);
            Span span = new Span(0, buffer.CurrentSnapshot.Length);
            buffer.Replace(span, result);

            var selection = EditorExtensionsPackage.DTE.ActiveDocument.Selection as TextSelection;
            selection.GotoLine(1);

            EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatDocument");
            EditorExtensionsPackage.DTE.UndoContext.Close();

            return true;
        }

        private string AddMissingVendorDeclarations(StringBuilder sb, CssEditorDocument doc, ICssSchemaInstance rootSchema)
        {
            var visitor = new CssItemCollector<Declaration>(true);
            doc.Tree.StyleSheet.Accept(visitor);

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
                }
            }

            return sb.ToString();
        }

        private string GetVendorDeclarations(IEnumerable<string> prefixes, Declaration declaration)
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
            return TextView.GetSelection("css").HasValue;
        }
    }
}