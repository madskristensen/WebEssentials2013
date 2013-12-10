using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Extensions.Text;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownOutlineTaggerProvider : ITaggerProvider
    {
        // Some people at Microsoft will recognize this.
        internal class ViewHostingControl : ContentControl
        {
            private readonly Func<IWpfTextView> createView;
            public ITextView TextView
            {
                get
                {
                    var wpfTextView = (IWpfTextView)base.Content;
                    if (wpfTextView == null)
                    {
                        wpfTextView = createView();
                        Content = wpfTextView.VisualElement;
                    }
                    return wpfTextView;
                }
            }
            public ViewHostingControl(Func<IWpfTextView> createView)
            {
                this.createView = createView;
                IsVisibleChanged += OnIsVisibleChanged;
                Background = Brushes.Transparent;
            }
            private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    if (Content == null)
                        Content = createView().VisualElement;
                }
                else
                {
                    TextView.Close();
                    Content = null;
                }
            }
            public override string ToString()
            {
                return TextView.TextBuffer.CurrentSnapshot.GetText();
            }
        }

        [Import]
        public ITextEditorFactoryService TextEditorFactory { get; set; }
        [Import]
        public IProjectionBufferFactoryService ProjectionFactory { get; set; }

        public IWpfTextView CreateTextView(IEnumerable<SnapshotSpan> lines)
        {
            var parentView = TextViewConnectionListener.GetTextViewDataForBuffer(lines.First().Snapshot.TextBuffer).LastActiveView;

            var buffer = ProjectionFactory.CreateProjectionBuffer(
                null,
                lines.SelectMany(s => new object[] { Environment.NewLine }.Concat(
                    // Use the text from the outer ProjectionBuffer, which can
                    // include language services from other projected buffers.
                    // This makes the tooltip include syntax highlighting that
                    // does not exist in the innermost Markdown buffer.
                    parentView.BufferGraph
                        .MapUpToBuffer(s, SpanTrackingMode.EdgeExclusive, parentView.TextBuffer)
                            .Select(s2 => s2.Snapshot.CreateTrackingSpan(s, SpanTrackingMode.EdgeExclusive))
                    ))
                    .Skip(1)    // Skip first newline
                    .ToList(),
                ProjectionBufferOptions.None
            );
            var view = TextEditorFactory.CreateTextView(buffer, TextEditorFactory.NoRoles);
            view.Background = Brushes.Transparent;
            SizeToFit(view);
            return view;
        }

        private static bool IsNormal(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static void SizeToFit(IWpfTextView view)
        {
            view.VisualElement.Height = view.LineHeight * view.TextBuffer.CurrentSnapshot.LineCount;
            view.LayoutChanged += (s, e) =>
            {
                view.VisualElement.Dispatcher.BeginInvoke(new Action(() =>
                {
                    double width = view.VisualElement.Width;
                    if (!IsNormal(view.MaxTextRightCoordinate))
                        return;
                    if (IsNormal(width) && view.MaxTextRightCoordinate <= width)
                        return;
                    view.VisualElement.Width = view.MaxTextRightCoordinate;
                }));
            };
        }


        public IOutliningRegionTag CreateTag(object collapsed, IEnumerable<SnapshotSpan> lines)
        {
            return new OutliningRegionTag(false, true, collapsed, new ViewHostingControl(() => CreateTextView(lines)));
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            // The classifier periodically recreates its ArtifactsCollection, so I need to pass a getter.
            var classifier = ServiceManager.GetService<HtmlClassifier>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownOutlineTagger(() => classifier.ArtifactCollection, CreateTag)) as ITagger<T>;
        }
    }

    public delegate IOutliningRegionTag OutlineTagCreator(object collapsed, IEnumerable<SnapshotSpan> lines);
    public class MarkdownOutlineTagger : ITagger<IOutliningRegionTag>
    {
        private readonly Func<ArtifactCollection> artifactsGetter;
        private readonly OutlineTagCreator tagCreator;
        public MarkdownOutlineTagger(ArtifactCollection artifacts, OutlineTagCreator tagCreator) : this(() => artifacts, tagCreator) { }
        public MarkdownOutlineTagger(Func<ArtifactCollection> artifactsGetter, OutlineTagCreator tagCreator)
        {
            this.artifactsGetter = artifactsGetter;
            this.tagCreator = tagCreator;
            // TODO: Forward events
        }

        static string Caption(CodeBlockInfo block)
        {
            if (string.IsNullOrEmpty(block.Language))
                return "[ Code Block ]";
            return "[ " + block.Language + " Code Block ]";
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!spans.Any())
                yield break;

            var artifacts = artifactsGetter();

            // Start from the first artifact after the start
            // position.  Since code blocks will always have
            // an end-boundary artifact, this will work even
            // if we start in the middle of a code block.
            int i = artifacts.GetFirstItemAfterOrAtPosition(spans[0].Start);
            if (i < 0)
                yield break;

            CodeBlockInfo lastBlock = null;
            for (; i < artifacts.Count; i++)
            {
                var cba = artifacts[i] as ICodeBlockArtifact;

                // Skip artifacts that aren't in code blocks, and
                // those in code blocks we have already returned.
                if (cba == null || cba.BlockInfo == lastBlock)
                    continue;

                lastBlock = cba.BlockInfo;

                // Skip single-line indented code blocks, but not single-line fenced code blocks
                if (lastBlock.CodeLines.Count == 1 && lastBlock.OuterEnd.End == lastBlock.CodeLines[0].End)
                    continue;

                yield return new TagSpan<IOutliningRegionTag>(
                    new SnapshotSpan(spans[0].Snapshot, Span.FromBounds(lastBlock.OuterStart.Start, lastBlock.OuterEnd.End)),
                    tagCreator(
                        Caption(lastBlock),
                        lastBlock.CodeLines
                                 .Select(a => a.InnerRange.ToSnapshotSpan(spans[0].Snapshot))
                                 .ToList()      // Force eager evaluation; this query is only enumerated when a tooltip is shown, so we need to grab the snapshot
                    )
                );
                if (lastBlock.OuterStart.Start > spans.Last().End)
                    break;  // If we have completely passed the requested range, stop.
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }
    }
}
