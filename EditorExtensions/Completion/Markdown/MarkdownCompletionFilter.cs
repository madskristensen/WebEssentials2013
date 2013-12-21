using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Completion.Markdown
{
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    [Order(After = "HTML Completion Source Provider")]
    [Name("Markdown Completion Filter Provider")]
    [Export(typeof(ICompletionSourceProvider))]
    public class MarkdownCompletionFilterProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new MarkdownCompletionFilter();
        }
    }

    public class MarkdownCompletionFilter : ICompletionSource
    {
        public MarkdownCompletionFilter()
        { }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            // Prevent " & " from auto-completing an entity.
            // Remove the handler after it first triggers to
            // allow the user to select or type an entity by
            // hand.

            // TODO: Suppress within HTML code blocks

            if (completionSets.Count != 1)
                return;
            var set = completionSets[0];

            // If the user types " &a<ctrl+space>", don't change anything
            var text = set.ApplicableTo.GetText(set.ApplicableTo.TextBuffer.CurrentSnapshot);
            if (text != "&")
                return;

            int invokeCount = 0;
            EventHandler<ValueChangedEventArgs<CompletionSelectionStatus>> handler = null;
            handler = (s, e) =>
            {
                // Wait until a value is first selected & ignore
                // recursive calls from out handler.
                if (!e.NewValue.IsSelected)
                    return;

                // When a completion session is first triggered,
                // it calls SelectBestMatch() twice; once inside
                // Start() and once from Filter(). Suppress both
                // of those, but don't touch anything afterwards
                if (++invokeCount >= 2)
                    set.SelectionStatusChanged -= handler;

                set.SelectionStatus = new CompletionSelectionStatus(null, false, false);
            };
            set.SelectionStatusChanged += handler;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
