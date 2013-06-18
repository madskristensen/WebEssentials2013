using CssSorter;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class CssSortProperties : CommandTargetBase
    {
        private DTE2 _dte;
        private readonly string[] _supported = new[] { "CSS", "LESS" };
        //private static uint[] _commandIds = new uint[] { PkgCmdIDList.sortCssProperties };

        public CssSortProperties(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidCssCmdSet, PkgCmdIDList.sortCssProperties)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            TextDocument doc = _dte.ActiveDocument.Object("TextDocument") as TextDocument;
            EditPoint edit = doc.StartPoint.CreateEditPoint();
            string text = SortProperties(edit.GetText(doc.EndPoint));

            _dte.UndoContext.Open("Sort All Properties");

            edit.ReplaceText(doc.EndPoint, text, (int)vsFindOptions.vsFindOptionsNone);
            EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatDocument");
            doc.Selection.MoveToPoint(doc.StartPoint);

            _dte.UndoContext.Close();

            return true;
        }

        private string SortProperties(string text)
        {
            Sorter sorter = new Sorter();

            if (Path.GetExtension(_dte.ActiveDocument.FullName) == ".css")
            {
                return sorter.SortStyleSheet(text);
            }
            else if (Path.GetExtension(_dte.ActiveDocument.FullName) == ".less")
            {
                return sorter.SortLess(text);
            }

            return text;
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