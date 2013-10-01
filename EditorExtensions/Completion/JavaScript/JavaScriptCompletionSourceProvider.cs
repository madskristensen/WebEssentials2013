using Microsoft.VisualStudio.Language.Intellisense;
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
                new ElementsByIdCompletionSource(classNames)
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
                if (userText.Last() != userText[0]) return; // If there is no closing quote, do nothing.

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

        abstract class StringCompletionSource
        {
            public abstract Span? GetInvocationSpan(string text, int linePosition);

            public abstract IEnumerable<Completion> GetEntries(char quoteChar, SnapshotPoint caret);
        }

        class UseDirectiveCompletionSource : StringCompletionSource
        {
            public override Span? GetInvocationSpan(string text, int linePosition)
            {
                var quote = text.SkipWhile(Char.IsWhiteSpace).FirstOrDefault();
                if (quote != '"' && quote != '\'')
                    return null;

                var startIndex = text.TakeWhile(Char.IsWhiteSpace).Count();
                var endIndex = linePosition;
                // Consume the auto-added close quote, if present.
                if (text[endIndex] == quote)
                    endIndex++;
                return Span.FromBounds(startIndex, endIndex);
            }

            static ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupIntrinsic, StandardGlyphItem.GlyphItemPublic);
            public override IEnumerable<Completion> GetEntries(char quoteChar, SnapshotPoint caret)
            {
                return new[] { "use strict", "use asm" }.Select(s => new Completion(
                    quoteChar + s + quoteChar + ";",
                    quoteChar + s + quoteChar + ";",
                    "Instructs that this block be processed in " + s.Substring(4) + " mode by supporting JS engines",
                    _glyph,
                    null)
                );
            }
        }

        abstract class FunctionCompletionSource : StringCompletionSource
        {
            protected abstract string FunctionName { get; }

            public override Span? GetInvocationSpan(string text, int linePosition)
            {
                // Find the quoted string inside function call
                int startIndex = text.LastIndexOf(FunctionName + "(", linePosition);
                if (startIndex < 0)
                    return null;
                startIndex += FunctionName.Length + 1;
                startIndex += text.Skip(startIndex).TakeWhile(Char.IsWhiteSpace).Count();

                if (linePosition <= startIndex || (text[startIndex] != '"' && text[startIndex] != '\''))
                    return null;

                var endIndex = text.IndexOf(text[startIndex] + ")", startIndex);
                if (endIndex < 0)
                    endIndex = startIndex + text.Skip(startIndex + 1).TakeWhile(c => Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c) || c == '-' || c == '_').Count() + 1;
                else if (linePosition > endIndex)
                    return null;

                // Consume the auto-added close quote, if present.
                // If range ends at the end of the line, we cannot
                // check this.
                if (endIndex < text.Length && text[endIndex] == text[startIndex])
                    endIndex++;


                return Span.FromBounds(startIndex, endIndex);
            }
        }

        class ElementsByTagNameCompletionSource : FunctionCompletionSource
        {
            protected override string FunctionName { get { return "getElementsByTagName"; } }

            static ReadOnlyCollection<string> tagNames = TagCompletionProvider.GetListEntriesCache()
                                        .Select(c => c.DisplayText)
                                        .Distinct()
                                        .OrderBy(s => s)
                                        .ToList()
                                        .AsReadOnly();

            public override IEnumerable<Completion> GetEntries(char quoteChar, SnapshotPoint caret)
            {
                return tagNames.Select(s => GenerateCompletion(s, quoteChar));
            }
        }
        class ElementsByClassNameCompletionSource : FunctionCompletionSource
        {
            ICssNameCache _classNames;
            public ElementsByClassNameCompletionSource(ICssNameCache classNames) { _classNames = classNames; }
            protected override string FunctionName { get { return "getElementsByClassName"; } }

            public override IEnumerable<Completion> GetEntries(char quoteChar, SnapshotPoint caret)
            {
                return _classNames.GetNames(new Uri(caret.Snapshot.TextBuffer.GetFileName()), caret, CssNameType.Class)
                    .Select(s => s.Name)
                    .Distinct()
                    .OrderBy(s => s)
                    .Select(s => GenerateCompletion(s, quoteChar));
            }
        }
        class ElementsByIdCompletionSource : FunctionCompletionSource
        {
            ICssNameCache _classNames;
            public ElementsByIdCompletionSource(ICssNameCache classNames) { _classNames = classNames; }

            protected override string FunctionName { get { return "getElementById"; } }

            public override IEnumerable<Completion> GetEntries(char quoteChar, SnapshotPoint caret)
            {
                return _classNames.GetNames(new Uri(caret.Snapshot.TextBuffer.GetFileName()), caret, CssNameType.Id)
                    .Select(s => s.Name)
                    .Distinct()
                    .OrderBy(s => s)
                    .Select(s => GenerateCompletion(s, quoteChar));
            }
        }

        private static Completion GenerateCompletion(string name, char quote)
        {
            return new Completion(quote + name + quote, quote + name + quote, null, _glyph, null);
        }

        public void Dispose()
        {

        }
    }
}
