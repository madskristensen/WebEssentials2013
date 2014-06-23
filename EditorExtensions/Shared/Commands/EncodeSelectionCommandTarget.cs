using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions
{
    internal class EncodeSelection : CommandTargetBase<CommandId>
    {
        private DTE2 _dte;
        private static readonly IReadOnlyDictionary<CommandId, Replacement> _commands = new Dictionary<CommandId, Replacement> {
            { CommandId.HtmlEncode, HttpUtility.HtmlEncode },
            { CommandId.HtmlDecode, HttpUtility.HtmlDecode },
            { CommandId.AttrEncode, HttpUtility.HtmlAttributeEncode },
            { CommandId.UrlEncode,  HttpUtility.UrlEncode },
            { CommandId.UrlDecode,  HttpUtility.UrlDecode },
            { CommandId.JsEncode,   HttpUtility.JavaScriptStringEncode }
        };

        private delegate string Replacement(string original);

        public EncodeSelection(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, _commands.Keys.ToArray())
        {
            _dte = WebEssentialsPackage.DTE;
        }

        protected override bool Execute(CommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return Replace(_commands[commandId]);
        }

        private bool Replace(Replacement callback)
        {
            TextDocument document = GetTextDocument();
            string replacement = callback(document.Selection.Text);

            using (WebEssentialsPackage.UndoContext((callback.Method.Name)))
                document.Selection.Insert(replacement, 0);

            return true;
        }

        private TextDocument GetTextDocument()
        {
            return _dte.ActiveDocument.Object("TextDocument") as TextDocument;
        }

        protected override bool IsEnabled()
        {
            return !TextView.Selection.IsEmpty;
        }
    }
}