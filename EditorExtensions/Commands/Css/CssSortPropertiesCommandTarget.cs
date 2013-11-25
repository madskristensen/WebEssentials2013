using CssSorter;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class CssSortProperties : CommandTargetBase
    {
        private DTE2 _dte;

        public CssSortProperties(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidCssCmdSet, PkgCmdIDList.sortCssProperties)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var point = TextView.GetSelection("css");
            if (point == null) return false;

            var buffer = point.Value.Snapshot.TextBuffer;

            _dte.UndoContext.Open("Sort All Properties");

            string result = SortProperties(buffer.CurrentSnapshot.GetText(), buffer.ContentType);
            Span span = new Span(0, buffer.CurrentSnapshot.Length);
            buffer.Replace(span, result);

            EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatDocument");
            var selection = EditorExtensionsPackage.DTE.ActiveDocument.Selection as TextSelection;
            selection.GotoLine(1);

            _dte.UndoContext.Close();

            return true;
        }

        private string SortProperties(string text, IContentType contentType)
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