using System;
using EnvDTE80;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class CssSelectBrowsers : CommandTargetBase<MinifyCommandId>
    {
        private DTE2 _dte;

        public CssSelectBrowsers(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, MinifyCommandId.SelectBrowsers)
        {
            _dte = WebEssentialsPackage.DTE;
        }

        protected override bool Execute(MinifyCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            BrowserSelector selector = new BrowserSelector();
            selector.ShowDialog();

            return true;
        }

        protected override bool IsEnabled()
        {
            if (TextView.GetSelection("css") == null)
                return false;
            var item = _dte.Solution.FindProjectItem(_dte.ActiveDocument.FullName);
            return item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName);
        }
    }
}