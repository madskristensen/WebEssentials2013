using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Core;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Extensions.Text;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    [Export(typeof(IClassifierProvider))]
    [Order(After = "Microsoft.Html.Editor.Classification.HtmlClassificationProvider")]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            // The classifier periodically recreates its ArtifactsCollection, so I need to pass a getter.
            HtmlClassifier classifier = null;
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownClassifier(() =>
                (classifier ?? (classifier = ServiceManager.GetService<HtmlClassifier>(textBuffer))).ArtifactCollection,
                Registry
            ));
        }
    }

    public class MarkdownClassifier : IClassifier
    {
        // The beginning of the content area of a line (after any quote blocks)
        const string lineBegin = @"(?:^|\r?\n|\r)(?:(?: {0,3}>)+ {0,3})?";

        private static readonly Regex _reBold = new Regex(@"(?<Value>(\*\*|__)[^\s](?:.*?[^\s])?\1)");
        private static readonly Regex _reItalic = new Regex(@"(?<Value>((?<!\*)\*(?!\*)|(?<!_)_(?!_))[^\s](?:.*?[^\s])?\1\b)");

        private static readonly Regex _reQuote = new Regex(lineBegin + @"( {0,3}>)+(?<Value> {0,3}[^\r\n]+)(?:$|\r?\n|\r)");

        private static readonly Regex _reHeader = new Regex(lineBegin + @"(?<Value>([#]{1,6})[^#\r\n]+(\1(?!#))?)");

        private readonly IClassificationType codeType;
        private readonly IReadOnlyCollection<Tuple<Regex, IClassificationType>> typeRegexes;
        private readonly Func<ArtifactCollection> artifactsGetter;

        public MarkdownClassifier(ArtifactCollection artifacts, IClassificationTypeRegistryService registry) : this(() => artifacts, registry) { }
        public MarkdownClassifier(Func<ArtifactCollection> artifactsGetter, IClassificationTypeRegistryService registry)
        {
            this.artifactsGetter = artifactsGetter;

            codeType = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownCode);
            typeRegexes = new[] {
                Tuple.Create(_reBold, registry.GetClassificationType(MarkdownClassificationTypes.MarkdownBold)),
                Tuple.Create(_reItalic, registry.GetClassificationType(MarkdownClassificationTypes.MarkdownItalic)),
                Tuple.Create(_reHeader, registry.GetClassificationType(MarkdownClassificationTypes.MarkdownHeader)),
                Tuple.Create(_reQuote, registry.GetClassificationType(MarkdownClassificationTypes.MarkdownQuote))
            };
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var results = new List<ClassificationSpan>();

            var artifacts = artifactsGetter();

            // In case the span starts in the middle
            // of an HTML code block, we must always
            // start from the previous artifact.
            int thisArtifact = artifacts.GetLastItemBeforeOrAtPosition(span.Start);

            // For each artifact, classify the artifact and
            // the plain text after it.  If the span starts
            // before the first artifact, the 1st iteration
            // will classify the leading text only.
            while (thisArtifact < artifacts.Count)
            {
                // If the span starts before the first artifact, thisArtifact will be negative.
                if (thisArtifact >= 0)
                    results.AddRange(ClassifyArtifacts(span.Snapshot, artifacts, ref thisArtifact));

                int plainStart = thisArtifact < 0 ? 0 : artifacts[thisArtifact].End;
                thisArtifact++;
                int plainEnd = thisArtifact == artifacts.Count ? span.Snapshot.Length : artifacts[thisArtifact].Start;

                // If artifacts become inconsistent, don't choke
                if (plainEnd <= plainStart)
                    continue;

                // Chop off any part before or after the span being classified
                var plainSpan = span.Intersection(Span.FromBounds(plainStart, plainEnd));

                // If there was no intersection, we've passed the target span and can stop.
                if (plainSpan == null) break;

                results.AddRange(ClassifyPlainSpan(plainSpan.Value));
            }

            return results;
        }

        private IEnumerable<ClassificationSpan> ClassifyPlainSpan(SnapshotSpan span)
        {
            var text = span.GetText();
            return typeRegexes.SelectMany(t => ClassifyMatches(span, text, t.Item1, t.Item2));
        }
        ///<summary>Classifies an entire code block from any artifact in the block.</summary>
        /// <param name="index">The index of the artifact that the classifier is up to.  The method will update this parameter to point to the last artifact in the block.</param>
        private IEnumerable<ClassificationSpan> ClassifyArtifacts(ITextSnapshot snapshot, ArtifactCollection artifacts, ref int index)
        {
            var blockArtifact = artifacts[index] as ICodeBlockArtifact;
            if (blockArtifact == null)
            {
                if (artifacts[index].TreatAs == ArtifactTreatAs.Code)
                    return ClassifyArtifact(snapshot, artifacts[index]);
                else
                    return Enumerable.Empty<ClassificationSpan>();
            }

            IEnumerable<IArtifact> toClassify = blockArtifact.BlockInfo.CodeLines;

            // Find the end of the artifacts for this code block
            for (; index < artifacts.Count; index++)
            {
                var boundary = artifacts[index] as BlockBoundaryArtifact;
                // If we reached the end boundary, consume it & stop
                if (boundary != null && boundary.Boundary == BoundaryType.End)
                    break;

                // If a non-code artifact is interspersed inside a
                // code block (eg, quote prefixes), handle it too.
                if (boundary == null && !(artifacts[index] is CodeLineArtifact))
                    toClassify = toClassify.Concat(new[] { artifacts[index] });
            }

            return toClassify.SelectMany(a => ClassifyArtifact(snapshot, a));
        }
        private IEnumerable<ClassificationSpan> ClassifyArtifact(ITextSnapshot snapshot, IArtifact artifact)
        {
            ITextRange range = artifact;
            // Don't highlight indent for indented code blocks
            if (artifact.LeftSeparator.Length > 0 && artifact.RightSeparator.Length == 0)
                range = artifact.InnerRange;
            yield return new ClassificationSpan(range.ToSnapshotSpan(snapshot), codeType);
        }

        private static IEnumerable<ClassificationSpan> ClassifyMatches(SnapshotSpan span, string text, Regex regex, IClassificationType type)
        {
            Match match = regex.Match(text);

            while (match.Success)
            {
                var value = match.Groups["Value"];
                var result = new SnapshotSpan(span.Snapshot, span.Start + value.Index, value.Length);
                yield return new ClassificationSpan(result, type);

                match = regex.Match(text, match.Index + match.Length);
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }
    }
}
