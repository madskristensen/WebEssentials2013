using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    internal class ReferenceTagGoToDefinition : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        public ReferenceTagGoToDefinition(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd97CmdID.GotoDefn)
        {
        }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var path = FindReferencedPath();
            if (path == null)
                return false;

            string referencedPath;

            if (path.StartsWith("~/", StringComparison.Ordinal))
                referencedPath = Path.Combine(ProjectHelpers.GetProjectFolder(TextView.TextBuffer.GetFileName()), path.Substring(2));
            else
                referencedPath = Path.Combine(Path.GetDirectoryName(TextView.TextBuffer.GetFileName()), path);

            if (referencedPath != null)
            {
                FileHelpers.OpenFileInPreviewTab(Path.GetFullPath(referencedPath));
                return true;
            }

            WebEssentialsPackage.DTE.StatusBar.Text = "Couldn't find " + path;

            return false;
        }


        static readonly Regex regex = new Regex(@"///\s*<reference\s+path=(['""])(?<path>[^'""]+)\1(\s*/>)?");
        private string FindReferencedPath()
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
            return FindReferencedPath() != null;
        }
    }
}