using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class RemoveDuplicateLines : CommandTargetBase
    {
        private DTE2 _dte;

        public RemoveDuplicateLines(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidEditorLinesCmdSet, PkgCmdIDList.RemoveDuplicateLines)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var span = GetSpan();
            var lines = span.GetText().Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            if (lines.Length == 0)
                return false;

            string result = RemoveDuplicates(lines);

            _dte.UndoContext.Open("Remove Duplicate Lines");
            TextView.TextBuffer.Replace(span.Span, result);
            _dte.UndoContext.Close();

            return true;
        }

        private static string RemoveDuplicates(string[] lines)
        {
            return string.Join(Environment.NewLine, lines.Distinct(new LineComparer()));
        }

        private class LineComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y))
                    return false;

                if (Object.ReferenceEquals(x, y)) return true;

                return string.Equals(x.Trim(), y.Trim(), StringComparison.CurrentCultureIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                if (Object.ReferenceEquals(obj, null)) return 0;

                return StringComparer.CurrentCultureIgnoreCase.GetHashCode(obj.Trim());
            }
        }

        private SnapshotSpan GetSpan()
        {
            var sel = TextView.Selection.StreamSelectionSpan;
            var start = new SnapshotPoint(TextView.TextSnapshot, sel.Start.Position).GetContainingLine().Start;
            var end = new SnapshotPoint(TextView.TextSnapshot, sel.End.Position).GetContainingLine().End;

            return new SnapshotSpan(start, end);
        }

        protected override bool IsEnabled()
        {
            return !TextView.Selection.IsEmpty;
        }
    }
}