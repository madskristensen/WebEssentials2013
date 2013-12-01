using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Less.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("CSS")]
    [TagType(typeof(TextMarkerTag))]
    public class CssHighlightWordTaggerProvider : IViewTaggerProvider
    {
        [Import]
        public ITextSearchService TextSearchService { get; set; }

        [Import]
        public ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            ITextStructureNavigator textStructureNavigator = TextStructureNavigatorSelector.GetTextStructureNavigator(textView.TextBuffer);

            return buffer.Properties.GetOrCreateSingletonProperty(() => new CssHighlightWordTagger(textView, buffer, TextSearchService, textStructureNavigator)) as ITagger<T>;
        }
    }

    internal class HighlightWordTag : TextMarkerTag
    {
        public HighlightWordTag() : base("MarkerFormatDefinition/HighlightWordFormatDefinition") { }
    }

    internal class CssHighlightWordTagger : ITagger<HighlightWordTag>
    {
        ITextView _view { get; set; }
        ITextBuffer _buffer { get; set; }
        ITextSearchService _textSearchService { get; set; }
        ITextStructureNavigator _textStructureNavigator { get; set; }
        NormalizedSnapshotSpanCollection _wordSpans { get; set; }
        SnapshotSpan? _currentWord { get; set; }
        SnapshotPoint _requestedPoint { get; set; }
        object _syncLock = new object();

        public CssHighlightWordTagger(ITextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService, ITextStructureNavigator textStructureNavigator)
        {
            this._view = view;
            this._buffer = sourceBuffer;
            this._textSearchService = textSearchService;
            this._textStructureNavigator = textStructureNavigator;
            this._wordSpans = new NormalizedSnapshotSpanCollection();
            this._currentWord = null;
            this._view.Caret.PositionChanged += CaretPositionChanged;
            this._view.LayoutChanged += ViewLayoutChanged;
        }

        private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.NewSnapshot != e.OldSnapshot)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => UpdateAtCaretPosition(_view.Caret.Position)), DispatcherPriority.ApplicationIdle, null);
            }
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => UpdateAtCaretPosition(e.NewPosition)), DispatcherPriority.ApplicationIdle, null);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            SnapshotPoint? point = caretPosition.Point.GetPoint(_buffer, caretPosition.Affinity);

            if (!point.HasValue)
                return;

            var doc = CssEditorDocument.FromTextBuffer(_buffer);
            ParseItem item = doc.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null)
                return;

            ParseItem validItem;
            validItem = item.FindType<ItemName>()
                     ?? item.FindType<ClassSelector>()
                     ?? item.FindType<IdSelector>()
                     ?? item.FindType<LessVariableName>()
                     ?? (ParseItem)item.FindType<LessMixinName>();

            if (validItem == null)
            {
                return;
                // There is no separate token type for a property name,
                // so I need to ensure that we are in the name portion.
                //var decl = item.FindType<Declaration>();
                //if (decl != null && item.Parent != decl.PropertyName)
                //    validItem = decl.PropertyName;
                //TODO: What was this for?
            }

            // If the new caret position is still within the current word (and on the same snapshot), we don't need to check it
            if (_currentWord.HasValue
                && _currentWord.Value.Snapshot == _view.TextSnapshot
                && point.Value >= _currentWord.Value.Start
                && point.Value <= _currentWord.Value.End)
            {
                return;
            }

            _requestedPoint = point.Value;
            Task.Run(new Action(() => UpdateWordAdornments(validItem)));
        }

        void UpdateWordAdornments(ParseItem item)
        {
            SnapshotPoint currentRequest = _requestedPoint;
            List<SnapshotSpan> wordSpans = new List<SnapshotSpan>();
            SnapshotSpan currentWord;

            if (item != null)
            {
                currentWord = new SnapshotSpan(new SnapshotPoint(_buffer.CurrentSnapshot, item.Start), item.Length);// word.Span;
                //If this is the current word, and the caret moved within a word, we're done.
                if (_currentWord.HasValue && currentWord == _currentWord)
                    return;

                //Find the new spans
                FindData findData = new FindData(item.Text, currentWord.Snapshot);
                findData.FindOptions = FindOptions.WholeWord | FindOptions.MatchCase;

                wordSpans.AddRange(_textSearchService.FindAll(findData));

                if (wordSpans.Count == 1)
                    wordSpans.Clear();
            }
            else
            {
                TextExtent word = _textStructureNavigator.GetExtentOfWord(currentRequest);
                currentWord = word.Span;
            }

            //If another change hasn't happened, do a real update
            if (currentRequest == _requestedPoint)
            {
                SynchronousUpdate(currentRequest, new NormalizedSnapshotSpanCollection(wordSpans), currentWord);
            }
        }

        private void SynchronousUpdate(SnapshotPoint currentRequest, NormalizedSnapshotSpanCollection newSpans, SnapshotSpan? newCurrentWord)
        {
            lock (_syncLock)
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

            // First, yield back the word the cursor is under (if it overlaps)
            // Note that we'll yield back the same word again in the wordspans collection;
            // the duplication here is expected.
            if (spans.OverlapsWith(new NormalizedSnapshotSpanCollection(currentWord)))
                yield return new TagSpan<HighlightWordTag>(currentWord, new HighlightWordTag());

            // Second, yield all the other words in the file
            foreach (SnapshotSpan span in NormalizedSnapshotSpanCollection.Overlap(spans, wordSpans))
            {
                yield return new TagSpan<HighlightWordTag>(span, new HighlightWordTag());
            }
        }
    }
}
