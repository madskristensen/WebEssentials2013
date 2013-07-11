using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
//using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    internal class HtmlGoToDefinition : CommandTargetBase
    {
        private HtmlEditorTree _tree;
        private string _path;

        public HtmlGoToDefinition(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(VSConstants.VSStd97CmdID).GUID, (uint)VSConstants.VSStd97CmdID.GotoDefn)
        {
            _tree = HtmlEditorDocument.FromTextView(textView).HtmlEditorTree;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            _path = _path.TrimStart('~').Trim();
            string openFile = EditorExtensionsPackage.DTE.ActiveDocument.FullName;
            string projectFolder = ProjectHelpers.GetProjectFolder(openFile);
            string absolute = ProjectHelpers.ToAbsoluteFilePath(_path, projectFolder);

            if (File.Exists(absolute))
            {
                OpenFileInPreviewTab(absolute);
                return true;
            }

            EditorExtensionsPackage.DTE.StatusBar.Text = "Couldn't find " + _path;

            return false;
        }

        private void OpenFileInPreviewTab(string file)
        {
            IVsNewDocumentStateContext newDocumentStateContext = null;
            
            try
            {
                IVsUIShellOpenDocument3 openDoc3 = EditorExtensionsPackage.GetGlobalService<SVsUIShellOpenDocument>() as IVsUIShellOpenDocument3;
                
                Guid reason = VSConstants.NewDocumentStateReason.Navigation;                
                newDocumentStateContext = openDoc3.SetNewDocumentState((uint)__VSNEWDOCUMENTSTATE.NDS_Provisional, ref reason);

                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(file);
            }
            finally
            {
                if (newDocumentStateContext != null)
                    newDocumentStateContext.Restore();
            }
        }

        private bool TryGetPath(out string path)
        {
            int position = TextView.Caret.Position.BufferPosition.Position;
            var point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, position);
            path = null;

            ElementNode element = null;
            AttributeNode attr = null;

            _tree.GetPositionElement(position, out element, out attr);

            if (element == null)
                return false;

            attr = element.GetAttribute("src") ?? element.GetAttribute("href");

            if (attr != null)
            {
                path = attr.Value;
                return true;
            }

            return false;
        }

        protected override bool IsEnabled()
        {
            return TryGetPath(out _path);
        }
    }
}