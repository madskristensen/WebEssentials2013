﻿using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Intellisense;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICompletionSourceProvider))]
    [Order(Before = "High")]
    [ContentType("JavaScript"),
    Name("EnhancedJavaScriptCompletion")]
    public class JavaScriptCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        private ICssNameCache _classNames = null;

        public ICompletionSource TryCreateCompletionSource(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new JavaScriptCompletionSource(buffer, _classNames)) as ICompletionSource;
        }
    }

    public class JavaScriptCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private static ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphXmlItem, StandardGlyphItem.GlyphItemPublic);

        public JavaScriptCompletionSource(ITextBuffer buffer, ICssNameCache classNames)
        {
            _buffer = buffer;

            completionSources = new ReadOnlyCollection<StringCompletionSource>(new StringCompletionSource[] {
                new UseDirectiveCompletionSource(), 
                new ElementsByTagNameCompletionSource(), 
                new ElementsByClassNameCompletionSource(classNames),
                new ElementsByIdCompletionSource(classNames),
                new NodeModuleCompletionSource()
            });
        }

        readonly ReadOnlyCollection<StringCompletionSource> completionSources;
        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            int position = session.TextView.Caret.Position.BufferPosition.Position;
            var line = _buffer.CurrentSnapshot.Lines.SingleOrDefault(l => l.Start <= position && l.End >= position);

            if (line == null)
                return;

            string text = line.GetText();
            var linePosition = position - line.Start;

            foreach (var source in completionSources)
            {
                var span = source.GetInvocationSpan(text, linePosition);
                if (span == null) continue;

                var trackingSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(span.Value.Start + line.Start, span.Value.Length, SpanTrackingMode.EdgeInclusive);
                completionSets.Add(new StringCompletionSet(
                    source.GetType().Name,
                    trackingSpan,
                    source.GetEntries(quoteChar: text[span.Value.Start], caret: session.TextView.Caret.Position.BufferPosition)
                ));
            }
            // TODO: Merge & resort all sets?  Will StringCompletionSource handle other entries?
            //completionSets.SelectMany(s => s.Completions).OrderBy(c=>c.DisplayText.TrimStart('"','\''))
        }
        ///<summary>A CompletionSet that selects matching completions even if the user text has an early closing quote.</summary>
        class StringCompletionSet : CompletionSet
        {
            public StringCompletionSet(string moniker, ITrackingSpan span, IEnumerable<Completion> completions) : base(moniker, "Web Essentials", span, completions, null) { }

            public override void SelectBestMatch()
            {
                base.SelectBestMatch();

                var snapshot = ApplicableTo.TextBuffer.CurrentSnapshot;
                var userText = ApplicableTo.GetText(snapshot);

                // If VS couldn't find an exact match, try again without closing quote.
                if (SelectionStatus.IsSelected) return;
                if (userText.Length == 0 || userText.Last() != userText[0]) return; // If there is no closing quote, do nothing.

                var originalSpan = ApplicableTo;
                try
                {
                    var spanPoints = originalSpan.GetSpan(snapshot);
                    ApplicableTo = snapshot.CreateTrackingSpan(spanPoints.Start, spanPoints.Length - 1, ApplicableTo.TrackingMode);
                    base.SelectBestMatch();
                }
                finally { ApplicableTo = originalSpan; }
            }
        }

        public void Dispose()
        {

        }
    }
}
