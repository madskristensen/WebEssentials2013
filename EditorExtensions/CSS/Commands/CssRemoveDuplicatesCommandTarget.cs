using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class CssRemoveDuplicates : CommandTargetBase<CssCommandId>
    {
        private SnapshotPoint? _point;

        public CssRemoveDuplicates(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, CssCommandId.RemoveDuplicates)
        { }

        protected override bool Execute(CssCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_point == null)
                return false;

            ITextBuffer buffer = _point.Value.Snapshot.TextBuffer;
            CssEditorDocument doc = CssEditorDocument.FromTextBuffer(buffer);
            StringBuilder sb = new StringBuilder(buffer.CurrentSnapshot.GetText());
            int scrollPosition = TextView.TextViewLines.FirstVisibleLine.Extent.Start.Position;

            using (WebEssentialsPackage.UndoContext("Remove Duplicate Properties"))
            {
                int count;
                bool hasChanged = RemoveDuplicateProperties(sb, doc, out count);

                if (hasChanged)
                    buffer.SetText(sb.ToString()
                                     .Replace("/* BEGIN EXTERNAL SOURCE */\r\n", string.Empty)
                                     .Replace("\r\n/* END EXTERNAL SOURCE */\r\n", string.Empty));

                if (TextView.Caret.Position.BufferPosition.Snapshot == buffer.CurrentSnapshot)
                    (WebEssentialsPackage.DTE.ActiveDocument.Selection as TextSelection).GotoLine(1);

                WebEssentialsPackage.ExecuteCommand("Edit.FormatDocument");
                TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, TextView.TextSnapshot.GetLineNumberFromPosition(scrollPosition));
                WebEssentialsPackage.DTE.StatusBar.Text = count + " duplicate properties removed";
            }

            return true;
        }

        private static bool RemoveDuplicateProperties(StringBuilder sb, CssEditorDocument doc, out int count)
        {
            bool hasChanged = false;
            var visitor = new CssItemCollector<RuleBlock>(true);

            doc.Tree.StyleSheet.Accept(visitor);
            count = 0;

            foreach (RuleBlock rule in visitor.Items.Reverse())
            {
                HashSet<string> list = new HashSet<string>();

                foreach (Declaration dec in rule.Declarations.Reverse())
                {
                    if (!list.Add(dec.Text))
                    {
                        sb.Remove(dec.Start, dec.Length);
                        count++;

                        hasChanged = true;
                    }
                }
            }

            return hasChanged;
        }

        protected override bool IsEnabled()
        {
            _point = TextView.GetSelection("css");

            return _point.HasValue;
        }
    }
}
