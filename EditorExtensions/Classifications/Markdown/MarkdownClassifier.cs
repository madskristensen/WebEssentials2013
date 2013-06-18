using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<MarkdownClassifier>(() => new MarkdownClassifier(Registry));
        }
    }

    public class MarkdownClassifier : IClassifier
    {
        private static readonly Regex _reItalic = new Regex(@"(?<!\*)\*(?!\*).+?(?<!\*)\*(?!\*)|(?<!_)_(?!_).+?(?<!_)_(?!_)");
        private static readonly Regex _reBold = new Regex(@"(\*\*|__).+?(\1)");
        private static readonly Regex _reHeader = new Regex(@"(?<!#)([#]{1,6})([^#]+)(\1(?!#))?");
        private static readonly Regex _reCode = new Regex(@"(`)([^`]+)(\1)");

        private IClassificationType _bold, _italic, _header, _code;

        public MarkdownClassifier(IClassificationTypeRegistryService registry)
        {
            _bold = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownBold);
            _italic = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownItalic);
            _header = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownHeader);
            _code = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownCode);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            string text = span.GetText();
            
            var bolds = FindMatches(span, text, _reBold, _bold);
            var italics = FindMatches(span, text, _reItalic, _italic);
            var headers = FindMatches(span, text, _reHeader, _header);
            var codes = FindMatches(span, text, _reCode, _code);
            
            return bolds.Concat(italics).Concat(headers).Concat(codes).ToList();
        }

        private IEnumerable<ClassificationSpan> FindMatches(SnapshotSpan span, string text, Regex regex, IClassificationType type)
        {
            Match match = regex.Match(text);

            while (match.Success)
            {
                var result = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                yield return new ClassificationSpan(result, type);

                match = regex.Match(text, match.Index + match.Length);
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    }

}
