using System;
using System.Collections.Generic;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class CssSmartTagger : ITagger<CssSmartTag>, IDisposable
    {
        private ITextView _textView;
        private ITextBuffer _textBuffer;
        private CssTree _tree;
        private bool _pendingUpdate;
        private ItemHandlerRegistry<ICssSmartTagProvider> _smartTagProviders;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public CssSmartTagger(ITextView textView, ITextBuffer textBuffer)
        {
            _textView = textView;
            _textBuffer = textBuffer;
            _pendingUpdate = true;

            _textView.Caret.PositionChanged += OnCaretPositionChanged;
            // [Mads] I've added this so the smart tags refreshes when buffer is manipulated
            _textBuffer.ChangedLowPriority += BufferChanged;

            _smartTagProviders = new ItemHandlerRegistry<ICssSmartTagProvider>();
        }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            EnsureInitialized();

            _pendingUpdate = true;
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs eventArgs)
        {
            EnsureInitialized();

            _pendingUpdate = true;
        }

        /// <summary>
        /// This must be delayed so that the TextViewConnectionListener
        /// has a chance to initialize the WebEditor host.
        /// </summary>
        public bool EnsureInitialized()
        {
            if (_tree == null && WebEditor.Host != null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_textBuffer);
                    _tree = document.Tree;

                    RegisterSmartTagProviders();

                    WebEditor.OnIdle += OnIdle;
                }
                catch (Exception)
                {
                }
            }

            return _tree != null;
        }

        private void RegisterSmartTagProviders()
        {
            IEnumerable<Lazy<ICssSmartTagProvider>> providers = ComponentLocatorWithOrdering<ICssSmartTagProvider>.ImportMany();

            foreach (Lazy<ICssSmartTagProvider> provider in providers)
            {
                _smartTagProviders.RegisterHandler(provider.Value.ItemType, provider.Value);
            }
        }

        private void OnIdle(object sender, EventArgs eventArgs)
        {
            Update();
        }

        private void Update()
        {
            if (_pendingUpdate)
            {
                if (TagsChanged != null)
                {
                    // Tell the editor that the tags in the whole buffer changed. It will call back into GetTags().

                    SnapshotSpan span = new SnapshotSpan(_textBuffer.CurrentSnapshot, new Span(0, _textBuffer.CurrentSnapshot.Length));
                    TagsChanged(this, new SnapshotSpanEventArgs(span));
                }

                _pendingUpdate = false;
            }
        }

        public IEnumerable<ITagSpan<CssSmartTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            List<ITagSpan<CssSmartTag>> tags = new List<ITagSpan<CssSmartTag>>();

            if (!EnsureInitialized())
            {
                return tags;
            }

            // Map view caret to the CSS buffer
            ProjectionSelectionHelper selectionHelpers = new ProjectionSelectionHelper(_textView, new[] { _textBuffer });
            SnapshotPoint? bufferPoint = selectionHelpers.MapFromViewToBuffer(_textView.Caret.Position.BufferPosition);

            if (bufferPoint.HasValue)
            {
                for (ParseItem currentItem = GetContextItem(_tree.StyleSheet, bufferPoint.Value.Position);
                    currentItem != null; currentItem = currentItem.Parent)
                {
                    IEnumerable<ICssSmartTagProvider> providers = _smartTagProviders.GetHandlers(currentItem.GetType());
                    List<ISmartTagAction> actions = new List<ISmartTagAction>();

                    if (providers != null && _textBuffer.CurrentSnapshot.Length >= currentItem.AfterEnd)
                    {
                        ITrackingSpan trackingSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(currentItem.Start, currentItem.Length, SpanTrackingMode.EdgeInclusive);

                        foreach (ICssSmartTagProvider provider in providers)
                        {
                            IEnumerable<ISmartTagAction> newActions = provider.GetSmartTagActions(currentItem, bufferPoint.Value.Position, trackingSpan, _textView);

                            if (newActions != null)
                            {
                                actions.AddRange(newActions);
                            }
                        }
                    }

                    if (actions.Count > 0)
                    {
                        SmartTagActionSet actionSet = new SmartTagActionSet(actions.AsReadOnly());
                        List<SmartTagActionSet> actionSets = new List<SmartTagActionSet>();
                        actionSets.Add(actionSet);

                        SnapshotSpan itemSpan = new SnapshotSpan(_textBuffer.CurrentSnapshot, currentItem.Start, currentItem.Length);
                        CssSmartTag smartTag = new CssSmartTag(actionSets.AsReadOnly());

                        tags.Add(new TagSpan<CssSmartTag>(itemSpan, smartTag));
                    }
                }
            }

            return tags;
        }

        /// <summary>
        /// This code was copied from CompletionEngine.cs in the CSS code. If this class gets
        /// copied into the CSS code, reuse that other function (CompletionEngine.GetCompletionContextLeafItem)
        /// </summary>
        private static ParseItem GetContextItem(StyleSheet styleSheet, int position)
        {
            // Look on both sides of the cursor for a context item.

            ParseItem prevItem = styleSheet.ItemBeforePosition(position) ?? styleSheet;
            ParseItem nextItem = styleSheet.ItemAfterPosition(position);

            if (position > prevItem.AfterEnd)
            {
                // Not touching the previous item, check its parents

                for (; prevItem != null; prevItem = prevItem.Parent)
                {
                    if (prevItem.IsUnclosed || prevItem.AfterEnd >= position)
                    {
                        break;
                    }
                }
            }

            // Only use the next item if the cursor is touching it, and it's not a comment
            if (nextItem != null && (position < nextItem.Start || nextItem.FindType<Comment>() != null))
            {
                nextItem = null;
            }

            // When two things touch the cursor inside of a selector, always prefer the previous item.
            // If this logic gets larger, consider a better design to choose between two items.
            if (nextItem != null &&
                prevItem != null &&
                prevItem.AfterEnd == position &&
                prevItem.FindType<SimpleSelector>() != null)
            {
                nextItem = null;
            }

            return nextItem ?? prevItem;
        }

        public void Dispose()
        {
            _tree = null;
            if (_textBuffer != null)
            {
                _textBuffer.ChangedLowPriority -= BufferChanged;
                _textBuffer = null;
            }

            if (_textView != null)
            {
                _textView.Caret.PositionChanged -= OnCaretPositionChanged;
                _textView = null;
            }
        }
    }
}
