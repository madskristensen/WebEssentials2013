using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;
using System;
using System.Collections.Generic;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    internal class HtmlGoToDefinition : CommandTargetBase
    {
        private HtmlEditorTree _tree;
        private List<string> _tags = new List<string>() { "script", "link" };
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
                EditorExtensionsPackage.DTE.ItemOperations.OpenFile(absolute);
                return true;
            }

            EditorExtensionsPackage.DTE.StatusBar.Text = "Couldn't find " + _path;

            return false;
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

            if (element.Name == "script" && element.GetAttribute("src") != null)
            {
                path = element.GetAttribute("src").Value;
                return true;
            }

            if (element.Name == "link" && element.GetAttribute("href") != null)
            {
                path = element.GetAttribute("href").Value;
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