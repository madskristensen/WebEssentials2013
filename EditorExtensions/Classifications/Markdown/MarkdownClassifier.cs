using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor.Extensions.Text;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            var artifacts = HtmlEditorDocument.FromTextBuffer(textBuffer).HtmlEditorTree.ArtifactCollection;
            return textBuffer.Properties.GetOrCreateSingletonProperty<MarkdownClassifier>(() => new MarkdownClassifier(artifacts, Registry));
        }
    }

    public class MarkdownClassifier : IClassifier
    {
        // The beginning of the content area of a line (after any quote blocks)
        const string lineBegin = @"^(?:(?: {0,3}>)+ {0,3})?";

        private static readonly Regex _reBold = new Regex(@"(?<Value>(\*\*|__)[^\s].+?[^\s]\1)");
        private static readonly Regex _reItalic = new Regex(@"(?<Value>((?<!\*)\*(?!\*)|(?<!_)_(?!_))[^\s].+?[^\s]\1\b)");

        private static readonly Regex _reQuote = new Regex(lineBegin + @"( {0,3}>)+(?<Value> {0,3}[^\r\n]+)\r?$", RegexOptions.Multiline);

        private static readonly Regex _reHeader = new Regex(lineBegin + @"(?<Value>([#]{1,6})[^#\r\n]+(\1(?!#))?)", RegexOptions.Multiline);

        private readonly IClassificationType codeType;
        private readonly IReadOnlyCollection<Tuple<Regex, IClassificationType>> typeRegexes;
        private readonly ArtifactCollection artifacts;

        public MarkdownClassifier(ArtifactCollection artifacts, IClassificationTypeRegistryService registry)
        {
            this.artifacts = artifacts;

            codeType = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownCode);
            typeRegexes = new[] {
                Tuple.Create(_reBold, registry.GetClassificationType(MarkdownClassificationTypes.MarkdownBold)),
                Tuple.Create(_reItalic, registry.GetClassificationType(MarkdownClassificationTypes.MarkdownItalic)),
                Tuple.Create(_reHeader, registry.GetClassificationType(MarkdownClassificationTypes.MarkdownHeader)),
                Tuple.Create(_reQuote, registry.GetClassificationType(MarkdownClassificationTypes.MarkdownQuote))
            };
        }

        // This does not work properly for multiline fenced code-blocks,
        // since we get each line separately.  If I can assume that this
        // always runs sequentially without skipping, I can add state to
        // track whether we're in a fenced block.
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var results = new List<ClassificationSpan>();

            int lastArtifact = artifacts.GetItemContainingUsingInclusion(span.Start, true);

            if (lastArtifact >= 0)
                results.AddRange(ClassifyArtifact(span.Snapshot, artifacts[lastArtifact]));
            else
                lastArtifact = artifacts.GetLastItemBeforeOrAtPosition(span.Start);

            while (true)
            {
                // Find the span between the previous artifact and the current one
                SnapshotSpan? plainSpan;
                // If there are no artifacts in the document, check the entire document
                if (artifacts.Count == 0)
                    plainSpan = span;
                // If the span starts before the first artifact, check until the first artifact.
                else if (lastArtifact < 0)
                    plainSpan = new SnapshotSpan(span.Snapshot, Span.FromBounds(0, artifacts[0].Start));
                // If we just checked the final artifact, check the rest of the document
                else if (lastArtifact >= artifacts.Count - 1)
                    plainSpan = new SnapshotSpan(span.Snapshot, Span.FromBounds(artifacts[lastArtifact].End, span.Snapshot.Length));
                // Otherwise, check between the two artifacts.
                else
                    plainSpan = new SnapshotSpan(span.Snapshot, Span.FromBounds(artifacts[lastArtifact].End, artifacts[lastArtifact + 1].Start));

                // Chop off any part before or after the span being classified
                plainSpan = plainSpan.Value.Intersection(span);

                // If there was no intersection, we've passed the target span and can stop.
                if (plainSpan == null) break;

                results.AddRange(ClassifyPlainSpan(plainSpan.Value));

                lastArtifact++;
                // If we're at the last artifact, stop after processing whatever text came after it.
                if (lastArtifact == artifacts.Count)
                    break;
                results.AddRange(ClassifyArtifact(span.Snapshot, artifacts[lastArtifact]));
            }

            return results;
        }

        private IEnumerable<ClassificationSpan> ClassifyPlainSpan(SnapshotSpan span)
        {
            var text = span.GetText();
            return typeRegexes.SelectMany(t => ClassifyMatches(span, text, t.Item1, t.Item2));
        }
        private IEnumerable<ClassificationSpan> ClassifyArtifact(ITextSnapshot snapshot, IArtifact artifact)
        {
            yield return new ClassificationSpan(artifact.ToSnapshotSpan(snapshot), codeType);
        }

        private IEnumerable<ClassificationSpan> ClassifyMatches(SnapshotSpan span, string text, Regex regex, IClassificationType type)
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
