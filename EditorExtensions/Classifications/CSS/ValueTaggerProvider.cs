using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("css")]
    [TagType(typeof(TextMarkerTag))]
    internal class VendorTaggerProvider : IViewTaggerProvider
    {

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null || !WESettings.Instance.Css.SyncVendorValues)
            {
                return null;
            }

            return buffer.Properties.GetOrCreateSingletonProperty(() => new VendorTagger(textView, buffer)) as ITagger<T>;
        }
    }

    internal class VendorTagger : ITagger<TextMarkerTag>
    {
        ITextView View { get; set; }
        ITextBuffer Buffer { get; set; }
        SnapshotPoint? CurrentChar { get; set; }
        private readonly VendorClassifier _vendorClassifier;
        private bool _pendingUpdate = false;

        internal VendorTagger(ITextView view, ITextBuffer buffer)
        {
            View = view;
            Buffer = buffer;
            CurrentChar = null;
            buffer.Properties.TryGetProperty(typeof(VendorClassifier), out _vendorClassifier);

            View.Caret.PositionChanged += CaretPositionChanged;
            View.LayoutChanged += ViewLayoutChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.NewSnapshot != e.OldSnapshot)
            {
                UpdateAtCaretPosition(View.Caret.Position);
            }
        }

        void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            UpdateAtCaretPosition(e.NewPosition);
        }

        void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            if (!_pendingUpdate)
            {
                _pendingUpdate = true;
                CurrentChar = caretPosition.Point.GetPoint(this.Buffer, caretPosition.Affinity);
                if (!CurrentChar.HasValue)
                {
                    return;
                }

                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => Update()), DispatcherPriority.ContextIdle);
            }
        }

        private void Update()
        {
            var tempEvent = TagsChanged;
            if (tempEvent != null)
            {
                SnapshotSpan span = new SnapshotSpan(this.Buffer.CurrentSnapshot, 0, this.Buffer.CurrentSnapshot.Length);
                tempEvent(this, new SnapshotSpanEventArgs(span));
                _pendingUpdate = false;
            }
        }

        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || Buffer.CurrentSnapshot.Length == 0)
                yield break;

            if (!CurrentChar.HasValue || CurrentChar.Value.Position >= CurrentChar.Value.Snapshot.Length)
                yield break;

            SnapshotPoint currentChar = CurrentChar.Value;
            if (spans[0].Snapshot != currentChar.Snapshot)
            {
                currentChar = currentChar.TranslateTo(spans[0].Snapshot, PointTrackingMode.Positive);
            }

            var allTags = _vendorClassifier.GetClassificationSpans(spans[0]).Where(s => s.ClassificationType.Classification == VendorClassificationTypes.Value);
            foreach (var tagSpan in allTags)
            {
                if (tagSpan.Span.Contains(currentChar))
                {
                    Declaration dec = _vendorClassifier.Cache.FirstOrDefault(e => currentChar.Position > e.Start && currentChar.Position < e.AfterEnd);
                    if (dec != null && dec.PropertyName.Text.Length > 0 && !dec.IsVendorSpecific())
                    {
                        foreach (Declaration vendor in _vendorClassifier.Cache.Where(d => d.Parent == dec.Parent && VendorClassifier.GetStandardName(d) == dec.PropertyName.Text))
                        {
                            // Manage quotes for -ms-filter
                            string value = Buffer.CurrentSnapshot.GetText(vendor.Colon.AfterEnd, vendor.AfterEnd - vendor.Colon.AfterEnd);
                            int quotes = value.StartsWith("'") || value.StartsWith("\"") ? 1 : 0;
                            SnapshotSpan vendorSpan = new SnapshotSpan(Buffer.CurrentSnapshot, vendor.Colon.AfterEnd + quotes, vendor.AfterEnd - vendor.Colon.AfterEnd - (quotes * 2));
                            yield return new TagSpan<TextMarkerTag>(vendorSpan, new TextMarkerTag("vendorhighlight"));
                        }

                        SnapshotSpan s = tagSpan.Span;
                        yield return new TagSpan<TextMarkerTag>(s, new TextMarkerTag("vendorhighlight"));
                        yield break;
                    }
                }
            }
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name("vendorhighlight")]
    [UserVisible(true)]
    internal class HighlightWordFormatDefinition : MarkerFormatDefinition
    {
        public HighlightWordFormatDefinition()
        {
            this.DisplayName = "CSS Property Value Highlight";
        }

    }
}