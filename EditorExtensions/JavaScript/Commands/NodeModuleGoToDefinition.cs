using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    internal class NodeModuleGoToDefinition : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        public NodeModuleGoToDefinition(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd97CmdID.GotoDefn)
        {
        }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var path = FindRequirePath();
            if (path == null)
                return false;

            var filePath = NodeModuleService.ResolveModule(Path.GetDirectoryName(TextView.TextBuffer.GetFileName()), path).ConfigureAwait(false).GetAwaiter().GetResult();

            if (filePath != null)
            {
                FileHelpers.OpenFileInPreviewTab(Path.GetFullPath(filePath));
                return true;
            }

            WebEssentialsPackage.DTE.StatusBar.Text = "Couldn't find " + path;

            return false;
        }


        static readonly Regex regex = new Regex(@"\brequire\s*\(\s*(['""])(?<path>[^""']+)\1\)?");
        private string FindRequirePath()
        {
            var position = TextView.Caret.Position.BufferPosition;
            var line = position.GetContainingLine();
            int linePos = position - line.Start.Position;

            var match = regex.Matches(line.GetText())
                             .Cast<Match>()
                             .FirstOrDefault(m => m.Index <= linePos && m.Index + m.Length >= linePos);
            if (match == null) return null;

            return match.Groups["path"].Value;
        }

        protected override bool IsEnabled()
        {
            return FindRequirePath() != null;
        }
    }
}