using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.Html.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace WebEssentialsTests
{
    [TestClass]
    public class MarkdownClassifierTests
    {
        private static readonly Func<string, Tuple<string, string>> Bold = CreateCreator(MarkdownClassificationTypes.MarkdownBold);
        private static readonly Func<string, Tuple<string, string>> Code = CreateCreator(MarkdownClassificationTypes.MarkdownCode);
        private static readonly Func<string, Tuple<string, string>> Header = CreateCreator(MarkdownClassificationTypes.MarkdownHeader);
        private static readonly Func<string, Tuple<string, string>> Italic = CreateCreator(MarkdownClassificationTypes.MarkdownItalic);
        private static readonly Func<string, Tuple<string, string>> Quote = CreateCreator(MarkdownClassificationTypes.MarkdownQuote);

        #region Helper Methods
        private static Func<string, Tuple<string, string>> CreateCreator(string classificationType) { return source => Tuple.Create(classificationType, source); }

        static readonly Regex newline = new Regex("\r*\n");
        private static List<Tuple<string, string>> Classify(string markdown, Span? subSpan = null)
        {
            if (subSpan == null)
            {
                var spanStart = markdown.IndexOf("(<");
                if (spanStart >= 0)
                {
                    markdown = markdown.Remove(spanStart, 2);
                    var spanEnd = markdown.IndexOf(">)", spanStart);
                    if (spanEnd < 0)
                        throw new ArgumentException("Markdown (<...>) span indicator must be well-formed", "markdown");
                    markdown = markdown.Remove(spanEnd, 2);
                    subSpan = Span.FromBounds(spanStart, spanEnd);
                }
            }

            var crlf = newline.Replace(markdown, "\r\n");
            var lf = newline.Replace(markdown, "\n");
            var cr = newline.Replace(markdown, "\r");

            // Test newline equivalence on the full source
            // string.  We cannot pass the subspan because
            // newline replacements will move its indices.
            // Also, this part is only to test the parser,
            // which doesn't get subspans anyway.
            var fullResult = RunParseCase(crlf);
            RunParseCase(lf).Should().Equal(fullResult, "LF should be the same as CRLF");
            RunParseCase(cr).Should().Equal(fullResult, "CR should be the same as CRLF");

            return RunParseCase(markdown, subSpan);
        }
        private static List<Tuple<string, string>> RunParseCase(string markdown, Span? subSpan = null)
        {

            var artifacts = new ArtifactCollection(new MarkdownCodeArtifactProcessor());
            artifacts.Build(markdown);
            var classifier = new MarkdownClassifier(artifacts, new MockClassificationTypeRegistry());
            return classifier.GetClassificationSpans(new SnapshotSpan(new MockSnapshot(markdown), subSpan ?? new Span(0, markdown.Length)))
                             .Select(cs => Tuple.Create(cs.ClassificationType.Classification, markdown.Substring(cs.Span.Start, cs.Span.Length)))
                             .ToList();

        }
        #endregion

        #region Helper Classes
        internal class MockClassificationTypeRegistry : IClassificationTypeRegistryService
        {
            public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes)
            {
                throw new NotImplementedException();
            }

            public IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes)
            {
                throw new NotImplementedException();
            }

            public IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes)
            {
                throw new NotImplementedException();
            }

            public IClassificationType GetClassificationType(string type)
            {
                return new MockType(type);
            }

            internal class MockType : IClassificationType
            {
                public IEnumerable<IClassificationType> BaseTypes { get { yield break; } }
                public string Classification { get; private set; }

                public MockType(string type)
                {
                    Classification = type;
                }

                public bool IsOfType(string type)
                {
                    return Classification.Equals(type, StringComparison.OrdinalIgnoreCase) || BaseTypes.Any(b => b.IsOfType(type));
                }
            }
        }
        #endregion

        [TestMethod]
        public void TestBasicClassifications()
        {
            Classify(@"**bold**, _italic_
#Header
>> ##Header2
").Should().Equal(
                Bold("**bold**"),
                Italic("_italic_"),
                Header("#Header"),
                Header("##Header2"),
                Quote(" ##Header2")
            );

            Classify("**bold**").Should().Equal(Bold("**bold**"));
            Classify("#Header").Should().Equal(Header("#Header"));
            Classify("  >  >  >Quote!").Should().Equal(Quote("Quote!"));
        }

        [TestMethod]
        public void TestCodeBlocks()
        {
            Classify(@"`code`").Should().Equal(Code("`code`"));
            Classify(@"`code` _ab_").Should().Equal(Code("`code`"), Italic("_ab_"));
            Classify(@"**a** `code`").Should().Equal(Bold("**a**"), Code("`code`"));

            Classify(@"`code` ... `more _code_!`").Should().Equal(Code("`code`"), Code("`more _code_!`"));
            Classify(@"`int *a;` ... `int* b = a;`").Should().Equal(Code("`int *a;`"), Code("`int* b = a;`"));
            Classify(@"
```cs
var x = 2;
var y = 3;").Should().Equal(Code("var x = 2;"), Code("var y = 3;"));

            Classify(@"    font-weight: _bold_;
    Normal line
      Furtherly indented line").Should().Equal(Code("font-weight: _bold_;"), Code("Normal line"), Code("  Furtherly indented line"));

            Classify(@"    Indented line
_italic_ `code`").Should().Equal(Code("Indented line"), Italic("_italic_"), Code("`code`"));

            Classify(@"
```html
<b>**ABC**!</b>
<code>`hi`</code>
```
**Bold**").Should().Equal(Code("<b>**ABC**!</b>"), Code("<code>`hi`</code>"), Bold("**Bold**"));
        }

        [TestMethod]
        public void TestPartialSpans()
        {
            Classify(@"**a** (< _b_ `cod>)e`").Should().Equal(Italic("_b_"), Code("`code`"));
            Classify(@"`co(<de` _b_  >)**a**").Should().Equal(Code("`code`"), Italic("_b_"));
            Classify(@"
    Earlier block

    Indented code block
    Some (<code
    More code!

**Bold**>)").Should().Equal(Code("Indented code block"), Code("Some code"), Code("More code!"), Bold("**Bold**"));
            Classify(@"
```html
<b>(<**ABC**!</b>
<code>`hi`</code>
```
**Bold** `a`>)").Should().Equal(Code("<b>**ABC**!</b>"), Code("<code>`hi`</code>"), Bold("**Bold**"), Code("`a`"));
        }
        // TODO: Expand partial span until newline to catch constructs containing span end.
        // TODO: Test quoted code blocks
    }
}