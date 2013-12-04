using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownOutlineTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            // The classifier periodically recreates its ArtifactsCollection, so I need to pass a getter.
            var classifier = ServiceManager.GetService<HtmlClassifier>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownOutlineTagger(() => classifier.ArtifactCollection)) as ITagger<T>;
        }
    }

    public class MarkdownOutlineTagger : ITagger<IOutliningRegionTag>
    {
        private readonly Func<ArtifactCollection> artifactsGetter;

        public MarkdownOutlineTagger(ArtifactCollection artifacts) : this(() => artifacts) { }
        public MarkdownOutlineTagger(Func<ArtifactCollection> artifactsGetter)
        {
            this.artifactsGetter = artifactsGetter;
            // TODO: Forward events
        }

        static string Caption(MarkdownCodeArtifact artifact)
        {
            if (string.IsNullOrEmpty(artifact.Language))
                return "[ Code Block ]";
            return "[ " + artifact.Language + " Code Block ]";
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!spans.Any())
                yield break;

            var artifacts = artifactsGetter();

            CodeBlockInfo blockStart = null;
            for (int i = 0; i < artifacts.Count; i++)
            {
                var mca = (MarkdownCodeArtifact)artifacts[i];
                // If we concluded a run of blocks, or if we're at the beginning, start the next run.
                if (blockStart == null || blockStart != mca.BlockInfo)
                {
                    var lastMCA = i == 0 ? null : (MarkdownCodeArtifact)artifacts[i - 1];
                    // If we concluded a block with more than one line (Artifact), tag it!
                    if (blockStart != null && lastMCA.BlockInfo == blockStart)
                        yield return new TagSpan<IOutliningRegionTag>(
                            new SnapshotSpan(spans[0].Snapshot, Span.FromBounds(blockStart.OuterStart, blockStart.OuterEnd)),
                            new OutliningRegionTag(false, true, Caption(lastMCA), String.Join(Environment.NewLine,
                                artifacts.Cast<MarkdownCodeArtifact>()
                                         .SkipWhile(a => a.BlockInfo != blockStart)
                                         .TakeWhile(a => !ReferenceEquals(a, mca))
                                         .Select(a => a.GetText(spans[0].Snapshot))
                    )));

                    // If we're at the beginning, skip to the first artifact in the requested range
                    if (blockStart == null)
                    {
                        i = artifacts.GetItemContainingUsingInclusion(spans[0].Start, true);
                        if (i < 0)
                            i = artifacts.GetFirstItemAfterOrAtPosition(spans[0].Start);
                        if (i < 0)
                            yield break;
                        mca = (MarkdownCodeArtifact)artifacts[i];
                        // Rewind to the beginning of this block so that we
                        // don't return partial blocks when the spans start
                        // in the middle of a code block.
                        while (i > 0 && artifacts[i - 1].Start > mca.BlockInfo.OuterStart)
                            i--;
                        mca = (MarkdownCodeArtifact)artifacts[i];
                    }
                    else if (mca.Start > spans.Last().End)
                        break;  // If we have completely passed the requested range, stop.
                    blockStart = mca.BlockInfo;
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }
    }
}
