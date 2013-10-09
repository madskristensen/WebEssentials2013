using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.ContainedLanguage;
using System.Reflection;
using Microsoft.Html.Core;
using Microsoft.Web.Core;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IContentTypeHandlerProvider))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownContentTypeHandlerProvider : IContentTypeHandlerProvider
    {
        [Import]
        readonly IContentTypeRegistryService contentTypeRegistry;

        public IContentTypeHandler GetContentTypeHandler()
        {
            return new MarkdownContentTypeHandler(contentTypeRegistry);
        }
    }

    public class MarkdownContentTypeHandler : HtmlContentTypeHandler
    {
        static readonly Func<HtmlContentTypeHandler, List<LanguageBlockHandler>> GetLanguageBlockHandlerList =
            (Func<HtmlContentTypeHandler, List<LanguageBlockHandler>>)
            Delegate.CreateDelegate(
                typeof(Func<HtmlContentTypeHandler, List<LanguageBlockHandler>>),
                typeof(HtmlContentTypeHandler).GetProperty("LanguageBlockHandlers", BindingFlags.NonPublic).GetGetMethod()
            );

        readonly IContentTypeRegistryService contentTypeRegistry;
        public MarkdownContentTypeHandler(IContentTypeRegistryService contentTypeRegistry)
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }

        protected override void CreateBlockHandlers()
        {
            base.CreateBlockHandlers();
        }

        public override void Init(HtmlEditorTree editorTree)
        {
            base.Init(editorTree);
            ContainedLanguageSettings.FormatOnPaste = false;
            ContainedLanguageSettings.EnableSyntaxCheck = false;
        }

        public override IContentType GetContentTypeOfLocation(int position)
        {
            int itemContaining = EditorTree.ArtifactCollection.GetItemContaining(position);
            if (itemContaining >= 0)
            {
                IArtifact artifact = EditorTree.ArtifactCollection[itemContaining];
                if (artifact.TreatAs == ArtifactTreatAs.Comment)
                {
                    return contentTypeRegistry.GetContentType("text");
                }
            }
            return base.GetContentTypeOfLocation(position);
        }
    }

    class CodeBlockBlockHandler : ArtifactBasedBlockHandler
    {
        protected override BufferGenerator CreateBufferGenerator()
        {
            throw new NotImplementedException();
        }
    }


    public class MarkdownCodeArtifactProcessor : IArtifactProcessor
    {
        public ArtifactCollection CreateArtifactCollection()
        {
            throw new NotImplementedException();
        }

        public void GetArtifacts(ITextProvider text, ArtifactCollection artifactCollection)
        {
            //TODO: Comment- or tag- based language prefixes
        }

        public bool IsReady { get { return true; } }

        public string LeftSeparator { get { return "`"; } }
        public string RightSeparator { get { return "`"; } }
        public string LeftCommentSeparator { get { return "<!--"; } }
        public string RightCommentSeparator { get { return "<!--"; } }
    }
    public class MarkdownCodeArtifact : Artifact
    {
        public MarkdownCodeArtifact(string language, ITextRange range, int leftLength, int rightLength)
            : base(ArtifactTreatAs.Code, range, leftLength, rightLength, language, true) { }
    }


    public class MarkdownClassifier : IClassifier
    {
        // A single inline code block
        const string inlineCodeBlock = @"`[^`\s](?:[^`\r\n]+[^`\s])?`";
        // A single fenced code block
        const string fencedCodeBlock = @"```[\s\S]+?^```";

        // The beginning of the content area of a line (after any quote blocks)
        const string lineBegin = @"^(?:(?: {0,3}>)+ {0,3})?";

        private static readonly Regex _reBold = new Regex(@"(?<Value>(\*\*|__)[^\s].+?[^\s]\1)");
        private static readonly Regex _reItalic = new Regex(@"(?<Value>((?<!\*)\*(?!\*)|(?<!_)_(?!_))[^\s].+?[^\s]\1\b)");

        // A multi-line fenced code block starting in a quote should all count as part of the quote
        private static readonly Regex _reQuote = new Regex(lineBegin + @"( {0,3}>)+(?<Value> {0,3}(" + fencedCodeBlock + @"|(?!(> {0,3})*```)).+$)", RegexOptions.Multiline);

        private static readonly Regex _reHeader = new Regex(lineBegin + @"(?<Value>([#]{1,6})[^#\r\n]+(\1(?!#))?)", RegexOptions.Multiline);
        private static readonly Regex _reCode = new Regex(
            @"(?<Value>" + inlineCodeBlock + @")|"                      // Inline code block
            + lineBegin + @"((?<= {3}) | {4})(?<Value>.+$)|"            // Indented code block (even inside quote blocks)
            + lineBegin + @"(?<Value>" + fencedCodeBlock + @")",        // GitHub-style fenced code block (RedCarpet)
            RegexOptions.Multiline);

        private IClassificationType _bold, _italic, _header, _code, _quote;

        public MarkdownClassifier(IClassificationTypeRegistryService registry)
        {
            _bold = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownBold);
            _italic = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownItalic);
            _header = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownHeader);
            _code = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownCode);
            _quote = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownQuote);
        }

        // This does not work properly for multiline fenced code-blocks,
        // since we get each line separately.  If I can assume that this
        // always runs sequentially without skipping, I can add state to
        // track whether we're in a fenced block.
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            string text = span.GetText();

            var codeBlocks = FindMatches(span, text, _reCode, _code).ToList();

            if (codeBlocks.Any())
            {
                // Flatten all code blocks to avoid matching text within them
                var nonCodeBuilder = text.ToCharArray();
                foreach (var code in codeBlocks)
                {
                    for (int i = code.Span.Start; i < code.Span.End; i++)
                    {
                        nonCodeBuilder[i - span.Start] = 'Q';
                    }
                }
                text = new String(nonCodeBuilder);
            }

            var quotes = FindMatches(span, text, _reQuote, _quote);
            var bolds = FindMatches(span, text, _reBold, _bold);
            var italics = FindMatches(span, text, _reItalic, _italic);
            var headers = FindMatches(span, text, _reHeader, _header);

            return bolds.Concat(italics).Concat(headers).Concat(codeBlocks).Concat(quotes).ToList();
        }

        private IEnumerable<ClassificationSpan> FindMatches(SnapshotSpan span, string text, Regex regex, IClassificationType type)
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
