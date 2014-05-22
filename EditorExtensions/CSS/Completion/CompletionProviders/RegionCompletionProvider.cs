using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Threading;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("RegionCompletionProvider")]
    internal class RegionCompletionProvider : ICssCompletionListProvider, ICssCompletionCommitListener
    {
        private RegionCompletionListEntry _entry = new RegionCompletionListEntry();

        public CssCompletionContextType ContextType
        {
            get { return CssCompletionContextType.ItemNameSelector; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            var line = context.Snapshot.GetLineFromPosition(context.ContextItem.Start);
            string text = line.GetText().Trim();

            if (text.Length == context.ContextItem.Length)
            {
                yield return _entry;
            }
        }

        public void OnCommitted(ICssCompletionListEntry entry, ITrackingSpan contextSpan, SnapshotPoint caret, ITextView textView)
        {
            if (entry.DisplayText == "Add region...")
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => System.Windows.Forms.SendKeys.Send("{TAB}")), DispatcherPriority.Normal);
            }
        }
    }
}