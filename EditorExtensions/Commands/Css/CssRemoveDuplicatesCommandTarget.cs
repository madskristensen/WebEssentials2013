using EnvDTE;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class CssRemoveDuplicates : CommandTargetBase
    {
        private DTE2 _dte;
        private readonly string[] _supported = new[] { "CSS", "LESS" };

        public CssRemoveDuplicates(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidCssCmdSet, PkgCmdIDList.cssRemoveDuplicates)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ITextBuffer buffer = ProjectHelpers.GetCurentTextBuffer();
            CssEditorDocument doc = new CssEditorDocument(buffer);

            StringBuilder sb = new StringBuilder(buffer.CurrentSnapshot.Length);
            sb.Append(buffer.CurrentSnapshot.GetText());

            EditorExtensionsPackage.DTE.UndoContext.Open("Remove Duplicate Properties");

            string result = RemoveDuplicateProperties(sb, doc);
            Span span = new Span(0, buffer.CurrentSnapshot.Length);
            buffer.Replace(span, result);

            var selection = EditorExtensionsPackage.DTE.ActiveDocument.Selection as TextSelection;
            selection.GotoLine(1);

            EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatDocument");
            EditorExtensionsPackage.DTE.UndoContext.Close();

            return true;
        }

        private string RemoveDuplicateProperties(StringBuilder sb, CssEditorDocument doc)
        {
            var visitor = new CssItemCollector<RuleBlock>(true);
            doc.Tree.StyleSheet.Accept(visitor);

            foreach (RuleBlock rule in visitor.Items.Reverse())
            {
                HashSet<string> list = new HashSet<string>();

                foreach (Declaration dec in rule.Declarations.Reverse())
                {
                    if (list.Contains(dec.Text))
                    {
                        sb.Remove(dec.Start, dec.Length);
                        continue;
                    }

                    list.Add(dec.Text);
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