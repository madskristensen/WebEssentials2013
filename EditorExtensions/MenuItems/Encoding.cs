//using System;
//using System.ComponentModel.Design;
//using System.Web;
//using EnvDTE;
//using EnvDTE80;
//using Microsoft.VisualStudio.Shell;

//namespace MadsKristensen.EditorExtensions
//{
//    internal class EncodingMenu
//    {
//        private DTE2 _dte;
//        private OleMenuCommandService _mcs;
//        private delegate string Replacement(string original);

//        public EncodingMenu(DTE2 dte, OleMenuCommandService mcs)
//        {
//            _dte = dte;
//            _mcs = mcs;
//        }

//        public void SetupCommands()
//        {
//            SetupCommand(PkgCmdIDList.htmlEncode, HttpUtility.HtmlEncode);
//            SetupCommand(PkgCmdIDList.attrEncode, HttpUtility.HtmlAttributeEncode);
//            SetupCommand(PkgCmdIDList.htmlDecode, HttpUtility.HtmlDecode);
//            SetupCommand(PkgCmdIDList.urlEncode, HttpUtility.UrlEncode);
//            SetupCommand(PkgCmdIDList.urlPathEncode, HttpUtility.UrlPathEncode);
//            SetupCommand(PkgCmdIDList.urlDecode, HttpUtility.UrlDecode);
//            SetupCommand(PkgCmdIDList.jsEncode, HttpUtility.JavaScriptStringEncode);
//        }

//        private void SetupCommand(uint command, Replacement callback)
//        {
//            CommandID commandId = new CommandID(GuidList.guidEditorExtensionsCmdSet, (int)command);
//            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Replace(callback), commandId);

//            menuCommand.BeforeQueryStatus += (s, e) =>
//            {
//                string selection = GetTextDocument().Selection.Text;
//                menuCommand.Enabled = selection.Length > 0 && callback(selection) != selection;
//            };

//            _mcs.AddCommand(menuCommand);
//        }

//        private TextDocument GetTextDocument()
//        {
//            return _dte.ActiveDocument.Object("TextDocument") as TextDocument;
//        }

//        private void Replace(Replacement callback)
//        {
//            TextDocument document = GetTextDocument();
//            string replacement = callback(document.Selection.Text);

//            _dte.UndoContext.Open(callback.Method.Name);
//            document.Selection.Insert(replacement, 0);
//            _dte.UndoContext.Close();
//        }
//    }
//}