using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace MadsKristensen.WebEssentials.Structures.Js
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("JavaScript")]
    [TagType(typeof(TextMarkerTag))]
    internal class HighlightWordTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView.TextBuffer != buffer)
                return null;

            ITextStructureNavigator textStructureNavigator = TextStructureNavigatorSelector.GetTextStructureNavigator(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new JavaScriptHighlightWordTagger(textView, buffer, TextSearchService, textStructureNavigator)) as ITagger<T>;
        }
    }

    internal class HighlightWordTag : TextMarkerTag
    {
        public HighlightWordTag() : base("MarkerFormatDefinition/HighlightWordFormatDefinition") { }
    }

    internal class JavaScriptHighlightWordTagger : ITagger<HighlightWordTag>
    {
        ITextView _view { get; set; }
        ITextBuffer _buffer { get; set; }
        ITextSearchService _textSearchService { get; set; }
        ITextStructureNavigator _textStructureNavigator { get; set; }
        NormalizedSnapshotSpanCollection _wordSpans { get; set; }
        SnapshotSpan? _currentWord { get; set; }
        SnapshotPoint _requestedPoint { get; set; }
        object _updateLock = new object();
        private bool _inProgress;

        public JavaScriptHighlightWordTagger(ITextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService, ITextStructureNavigator textStructureNavigator)
        {
            this._view = view;
            this._buffer = sourceBuffer;
            this._textSearchService = textSearchService;
            this._textStructureNavigator = textStructureNavigator;
            this._wordSpans = new NormalizedSnapshotSpanCollection();
            this._currentWord = null;
            this._view.Caret.PositionChanged += CaretPositionChanged;
        }

        void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (!_inProgress)
            {
                _inProgress = true;

                Task.Run(() =>
                {
                    UpdateAtCaretPosition(e.NewPosition);
                    _inProgress = false;
                });
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            SnapshotPoint? point = caretPosition.Point.GetPoint(_buffer, caretPosition.Affinity);

            if (!point.HasValue)
                return;

            // If the new caret position is still within the current word (and on the same snapshot), we don't need to check it
            if (_currentWord.HasValue
                && _currentWord.Value.Snapshot == _view.TextSnapshot
                && point.Value >= _currentWord.Value.Start
                && point.Value <= _currentWord.Value.End)
            {
                return;
            }

            _requestedPoint = point.Value;

            UpdateWordAdornments();
        }

        void UpdateWordAdornments()
        {
            SnapshotPoint currentRequest = _requestedPoint;
            List<SnapshotSpan> wordSpans = new List<SnapshotSpan>();
            //Find all words in the buffer like the one the caret is on
            TextExtent word = _textStructureNavigator.GetExtentOfWord(currentRequest);
            bool foundWord = true;
            //If we've selected something not worth highlighting, we might have missed a "word" by a little bit
            if (!WordExtentIsValid(currentRequest, word))
            {
                //Before we retry, make sure it is worthwhile
                if (word.Span.Start != currentRequest
                     || currentRequest == currentRequest.GetContainingLine().Start
                     || char.IsWhiteSpace((currentRequest - 1).GetChar()))
                {
                    foundWord = false;
                }
                else
                {
                    // Try again, one character previous. 
                    //If the caret is at the end of a word, pick up the word.
                    word = _textStructureNavigator.GetExtentOfWord(currentRequest - 1);

                    //If the word still isn't valid, we're done
                    if (!WordExtentIsValid(currentRequest, word))
                        foundWord = false;
                }
            }

            if (!foundWord)
            {
                //If we couldn't find a word, clear out the existing markers
                SynchronousUpdate(currentRequest, new NormalizedSnapshotSpanCollection(), null);
                return;
            }

            SnapshotSpan currentWord = word.Span;
            //If this is the current word, and the caret moved within a word, we're done.
            if (_currentWord.HasValue && currentWord == _currentWord)
                return;

            //Find the new spans
            FindData findData = new FindData(currentWord.GetText(), currentWord.Snapshot);
            findData.FindOptions = FindOptions.WholeWord | FindOptions.MatchCase;

            wordSpans.AddRange(_textSearchService.FindAll(findData));

            //If another change hasn't happened, do a real update
            if (currentRequest == _requestedPoint)
                SynchronousUpdate(currentRequest, new NormalizedSnapshotSpanCollection(wordSpans), currentWord);
        }
        static bool WordExtentIsValid(SnapshotPoint currentRequest, TextExtent word)
        {
            return word.IsSignificant
                && currentRequest.Snapshot.GetText(word.Span).Any(c => char.IsLetter(c));
        }

        void SynchronousUpdate(SnapshotPoint currentRequest, NormalizedSnapshotSpanCollection newSpans, SnapshotSpan? newCurrentWord)
        {
            lock (_updateLock)
            {
                if (currentRequest != _requestedPoint)
                    return;

                _wordSpans = newSpans;
                _currentWord = newCurrentWord;

                var tempEvent = TagsChanged;
                if (tempEvent != null)
                    tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)));
            }
        }

        public IEnumerable<ITagSpan<HighlightWordTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (_currentWord == null)
                yield break;

            // Hold on to a "snapshot" of the word spans and current word, so that we maintain the same
            // collection throughout
            SnapshotSpan currentWord = _currentWord.Value;
            NormalizedSnapshotSpanCollection wordSpans = _wordSpans;

            if (spans.Count == 0 || _wordSpans.Count == 0)
                yield break;

            // If the requested snapshot isn't the same as the one our words are on, translate our spans to the expected snapshot
            if (spans[0].Snapshot != wordSpans[0].Snapshot)
            {
                wordSpans = new NormalizedSnapshotSpanCollection(
                    wordSpans.Select(span => span.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive)));

                currentWord = currentWord.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive);
            }

            NormalizedSnapshotSpanCollection words = NormalizedSnapshotSpanCollection.Overlap(spans, wordSpans);

            if (words.Count == 1)
                yield break;

            // First, yield back the word the cursor is under (if it overlaps)
            // Note that we'll yield back the same word again in the wordspans collection;
            // the duplication here is expected.
            if (spans.OverlapsWith(new NormalizedSnapshotSpanCollection(currentWord)))
                yield return new TagSpan<HighlightWordTag>(currentWord, new HighlightWordTag());

            // Second, yield all the other words in the file
            foreach (SnapshotSpan span in words)
            {
                yield return new TagSpan<HighlightWordTag>(span, new HighlightWordTag());
            }
        }
    }
}
