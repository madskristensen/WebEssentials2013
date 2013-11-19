using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class CssAddMissingVendor : CommandTargetBase
    {
        private DTE2 _dte;
        private readonly string[] _supported = new[] { "CSS", "LESS" };

        public CssAddMissingVendor(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidCssCmdSet, PkgCmdIDList.addMissingVendor)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ITextBuffer buffer = ProjectHelpers.GetCurentTextBuffer();
            CssEditorDocument doc = new CssEditorDocument(buffer);
            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);

            StringBuilder sb = new StringBuilder(buffer.CurrentSnapshot.Length);
            sb.Append(buffer.CurrentSnapshot.GetText());

            EditorExtensionsPackage.DTE.UndoContext.Open("Add Missing Vendor Specifics");

            string result = AddMissingVendorDeclarations(sb, doc, rootSchema);
            Span span = new Span(0, buffer.CurrentSnapshot.Length);
            buffer.Replace(span, result);

            var selection = EditorExtensionsPackage.DTE.ActiveDocument.Selection as TextSelection;
            selection.GotoLine(1);

            EditorExtensionsPackage.ExecuteCommand("Edit.FormatDocument");
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
            var buffer = ProjectHelpers.GetCurentTextBuffer();

            if (buffer != null && _supported.Contains(buffer.ContentType.DisplayName.ToUpperInvariant()))
            {
                return true;
            }

            return false;
        }
    }
}