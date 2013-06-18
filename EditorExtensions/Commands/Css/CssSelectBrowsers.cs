using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class CssSelectBrowsers : CommandTargetBase
    {
        private DTE2 _dte;
        private List<string> _possible = new List<string>() { ".CSS", ".LESS", ".SCSS" };

        public CssSelectBrowsers(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidMinifyCmdSet, PkgCmdIDList.SelectBrowsers)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            BrowserSelector selector = new BrowserSelector();
            selector.ShowDialog();            

            return true;
        }

        private bool IsValidTextBuffer(IWpfTextView view)
        {
            var projection = view.TextBuffer as IProjectionBuffer;

            if (projection != null)
            {
                int position = view.Caret.Position.BufferPosition.Position;
                var snapshotPoint = view.Caret.Position.BufferPosition;

                var buffers = projection.SourceBuffers.Where(s => s.ContentType.IsOfType("css"));

                foreach (ITextBuffer buffer in buffers)
                {
                    SnapshotPoint? point = view.BufferGraph.MapDownToBuffer(snapshotPoint, PointTrackingMode.Negative, buffer, PositionAffinity.Predecessor);

                    if (point.HasValue)
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        protected override bool IsEnabled()
        {
            var item = _dte.Solution.FindProjectItem(_dte.ActiveDocument.FullName);
            bool hasProject = item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName);

            if (hasProject && TextView != null && IsValidTextBuffer(TextView))
            {
                return true;
            }

            return false;
        }
    }
}