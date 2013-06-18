using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Web;

namespace MadsKristensen.EditorExtensions
{
    internal class EncodeSelection : CommandTargetBase
    {
        private DTE2 _dte;
        private static uint[] _commandIds = new uint[] {
            PkgCmdIDList.htmlEncode,
            PkgCmdIDList.htmlDecode,
            PkgCmdIDList.attrEncode,
            PkgCmdIDList.urlEncode,
            PkgCmdIDList.urlDecode,
            PkgCmdIDList.urlPathEncode,
            PkgCmdIDList.jsEncode,
        };

        private delegate string Replacement(string original);

        public EncodeSelection(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidEditorExtensionsCmdSet, _commandIds)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            switch (commandId)
            {
                case PkgCmdIDList.htmlEncode:
                    return Replace(HttpUtility.HtmlEncode);
                case PkgCmdIDList.htmlDecode:
                    return Replace(HttpUtility.HtmlDecode);
                case PkgCmdIDList.attrEncode:
                    return Replace(HttpUtility.HtmlAttributeEncode);
                case PkgCmdIDList.urlEncode:
                    return Replace(HttpUtility.UrlEncode);
                case PkgCmdIDList.urlDecode:
                    return Replace(HttpUtility.UrlDecode);
                case PkgCmdIDList.urlPathEncode:
                    return Replace(HttpUtility.UrlPathEncode);
                case PkgCmdIDList.jsEncode:
                    return Replace(HttpUtility.JavaScriptStringEncode);
            }

            return true;
        }

        private bool Replace(Replacement callback)
        {
            TextDocument document = GetTextDocument();
            string replacement = callback(document.Selection.Text);

            _dte.UndoContext.Open(callback.Method.Name);
            document.Selection.Insert(replacement, 0);
            _dte.UndoContext.Close();

            return true;
        }

        private TextDocument GetTextDocument()
        {
            return _dte.ActiveDocument.Object("TextDocument") as TextDocument;
        }

        protected override bool IsEnabled()
        {
            if (TextView != null && TextView.Selection.SelectedSpans.Count > 0)
            {
                return TextView.Selection.SelectedSpans[0].Length > 0;
            }

            return false;
        }
    }
}