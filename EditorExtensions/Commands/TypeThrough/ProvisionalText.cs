using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class HtmlProvisionalTextHighlightFactory : IWpfTextViewCreationListener
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("HtmlProvisionalTextHighlight")]
        [Order(Before = PredefinedAdornmentLayers.Outlining)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        public AdornmentLayerDefinition EditorAdornmentLayer { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
        }
    }

    public class ProvisionalText
    {
        public static bool IgnoreChange { get; set; }

        public event EventHandler<EventArgs> OnClose;
        public char ProvisionalChar { get; private set; }
        public ITrackingSpan TrackingSpan { get; private set; }

        private ITextView _textView;
        private IAdornmentLayer _layer;
        private Path _highlightAdornment;
        private Brush _highlightBrush;
        private bool _overtype = false;
        private bool _delete = false;
        private bool _projectionsChanged = false;
        private bool _adornmentRemoved = false;
        private IProjectionBuffer _projectionBuffer;

        public ProvisionalText(ITextView textView, Span textSpan)
        {
            IgnoreChange = false;

            _textView = textView;

            var wpfTextView = _textView as IWpfTextView;
            _layer = wpfTextView.GetAdornmentLayer("HtmlProvisionalTextHighlight");

            var textBuffer = _textView.TextBuffer;
            var snapshot = textBuffer.CurrentSnapshot;
            var provisionalCharSpan = new Span(textSpan.End - 1, 1);

            TrackingSpan = snapshot.CreateTrackingSpan(textSpan, SpanTrackingMode.EdgeExclusive);
            _textView.Caret.PositionChanged += OnCaretPositionChanged;

            textBuffer.Changed += OnTextBufferChanged;
            textBuffer.PostChanged += OnPostChanged;

            var _projectionBuffer = _textView.TextBuffer as IProjectionBuffer;
            if (_projectionBuffer != null)
            {
                _projectionBuffer.SourceSpansChanged += OnSourceSpansChanged;
            }

            Color highlightColor = SystemColors.HighlightColor;
            Color baseColor = Color.FromArgb(96, highlightColor.R, highlightColor.G, highlightColor.B);
            _highlightBrush = new SolidColorBrush(baseColor);

            ProvisionalChar = snapshot.GetText(provisionalCharSpan)[0];
            HighlightSpan(provisionalCharSpan.Start);
        }

        public Span CurrentSpan
        {
            get
            {
                return TrackingSpan.GetSpan(_textView.TextBuffer.CurrentSnapshot);
            }
        }

        private void EndTracking()
        {
            if (_textView != null)
            {
                ClearHighlight();

                if (_projectionBuffer != null)
                {
                    _projectionBuffer.SourceSpansChanged -= OnSourceSpansChanged;
                    _projectionBuffer = null;
                }

                if (_projectionsChanged || _adornmentRemoved)
                {
                    _projectionsChanged = false;
                    _adornmentRemoved = false;
                }

                _textView.TextBuffer.Changed -= OnTextBufferChanged;
                _textView.TextBuffer.PostChanged -= OnPostChanged;

                _textView.Caret.PositionChanged -= OnCaretPositionChanged;
                _textView = null;

                if (OnClose != null)
                    OnClose(this, EventArgs.Empty);
            }
        }

        public bool IsPositionInSpan(int position)
        {
            if (_textView != null)
            {
                if (CurrentSpan.Contains(position) && position > CurrentSpan.Start)
                    return true;
            }

            return false;
        }

        private void OnSourceSpansChanged(object sender, ProjectionSourceSpansChangedEventArgs e)
        {
            _projectionsChanged = true;
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => ResoreHighlight()));
        }

        void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            // If caret moves outside of the text tracking span, consider text final
            var position = _textView.Caret.Position.BufferPosition;

            if (!CurrentSpan.Contains(position) || position == CurrentSpan.Start)
            {
                EndTracking();
            }
        }

        void OnPostChanged(object sender, EventArgs e)
        {
            if (_textView != null && !IgnoreChange && !_projectionsChanged)
            {
                if (_overtype || _delete)
                {
                    _textView.TextBuffer.Replace(new Span(CurrentSpan.End - 1, 1), String.Empty);
                    EndTracking();
                }
                else
                {
                    HighlightSpan(CurrentSpan.End - 1);
                }
            }
        }

        void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // Zero changes typically means secondary buffer regeneration
            if (e.Changes.Count == 0)
            {
                _projectionsChanged = true;
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => ResoreHighlight()));
            }

            if (_textView != null && !IgnoreChange && !_projectionsChanged)
            {
                // If there is a change outside text span or change over provisional
                // text, we are done here: commit provisional text and disconnect.

                if (CurrentSpan.Length > 0 && e.Changes.Count == 1)
                {
                    var change = e.Changes[0];

                    if (CurrentSpan.Contains(change.OldSpan))
                    {
                        // Check provisional text overtype
                        if (change.OldLength == 0 && change.NewLength == 1 && change.OldPosition == CurrentSpan.End - 2)
                        {
                            char ch = _textView.TextBuffer.CurrentSnapshot.GetText(change.NewPosition, 1)[0];

                            if (ch == ProvisionalChar)
                                _overtype = true;
                        }
                        else if (change.NewLength > 0 && change.NewText.Last() == ProvisionalChar)//(change.NewLength == 0 && change.OldLength > 0 && change.OldPosition == CurrentSpan.Start)
                        {
                            // Deleting open quote or brace should also delete provisional character
                            _delete = true;
                        }

                        return;
                    }
                }

                EndTracking();
            }
        }

        private void ResoreHighlight()
        {
            if (_textView != null && (_projectionsChanged || _adornmentRemoved))
            {
                HighlightSpan(CurrentSpan.End - 1);
            }

            _projectionsChanged = false;
            _adornmentRemoved = false;
        }

        void HighlightSpan(int bufferPosition)
        {
            ClearHighlight();

            var wpfTextView = _textView as IWpfTextView;
            var snapshotSpan = new SnapshotSpan(wpfTextView.TextBuffer.CurrentSnapshot, new Span(bufferPosition, 1));

            Geometry highlightGeometry = wpfTextView.TextViewLines.GetTextMarkerGeometry(snapshotSpan);
            if (highlightGeometry != null)
            {
                _highlightAdornment = new Path();
                _highlightAdornment.Data = highlightGeometry;
                _highlightAdornment.Fill = _highlightBrush;
            }

            if (_highlightAdornment != null)
            {
                _layer.AddAdornment(
                    AdornmentPositioningBehavior.TextRelative, snapshotSpan,
                    this, _highlightAdornment, new AdornmentRemovedCallback(OnAdornmentRemoved));
            }
        }

        private bool _removing = false;

        private void OnAdornmentRemoved(object tag, UIElement element)
        {
            if (_removing)
                return;

            _adornmentRemoved = true;
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => ResoreHighlight()));
        }

        private void ClearHighlight()
        {
            if (_highlightAdornment != null)
            {
                _removing = true;

                _layer.RemoveAdornment(_highlightAdornment);
                _highlightAdornment = null;

                _removing = false;
            }
        }
    }
}
