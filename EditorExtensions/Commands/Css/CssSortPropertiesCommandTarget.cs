using System;
using CssSorter;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    internal class CssSortProperties : CommandTargetBase
    {
        public CssSortProperties(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, CommandGuids.guidCssCmdSet, CommandId.SortCssProperties)
        {
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("css");
            if (point == null) return false;

            var buffer = point.Value.Snapshot.TextBuffer;

            using (EditorExtensionsPackage.UndoContext("Sort All Properties"))
            {
                string result = SortProperties(buffer.CurrentSnapshot.GetText(), buffer.ContentType);
                Span span = new Span(0, buffer.CurrentSnapshot.Length);
                buffer.Replace(span, result);

                EditorExtensionsPackage.ExecuteCommand("Edit.FormatDocument");
                var selection = EditorExtensionsPackage.DTE.ActiveDocument.Selection as TextSelection;
                selection.GotoLine(1);
            }

            return true;
        }

        private static string SortProperties(string text, IContentType contentType)
        {
            Sorter sorter = new Sorter();

            if (contentType.IsOfType("LESS"))
                return sorter.SortLess(text);

            return sorter.SortStyleSheet(text);
        }

        protected override bool IsEnabled()
        {
            return TextView.GetSelection("css").HasValue;
        }
    }
}