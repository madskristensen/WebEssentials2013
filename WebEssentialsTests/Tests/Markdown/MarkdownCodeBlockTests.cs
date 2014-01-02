using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAssertions;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class MarkdownParserTests
    {
        #region Helper Methods
        static readonly Regex newline = new Regex("\r*\n");
        private static List<string> ParseCodeBlocks(string markdown)
        {
            var crlf = newline.Replace(markdown, "\r\n");
            var lf = newline.Replace(markdown, "\n");
            var cr = newline.Replace(markdown, "\r");

            var result = RunParseCase(crlf);
            RunParseCase(lf).Should().Equal(result, "LF should be the same as CRLF");
            RunParseCase(cr).Should().Equal(result, "CR should be the same as CRLF");

            return result;
        }
        private static List<string> RunParseCase(string markdown)
        {
            var retVal = new List<string>();
            var parser = new MarkdownParser(new TabAwareCharacterStream(markdown));
            parser.ArtifactFound += (s, e) => retVal.Add(markdown.Substring(e.Artifact.InnerRange.Start, e.Artifact.InnerRange.Length));
            parser.Parse();
            return retVal;
        }
        #endregion

        [TestMethod]
        public void TestInlineCodeBlocks()
        {
            ParseCodeBlocks(@"Hi there! `abc` Bye!").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@"Hi there! `abc``def` Bye!").Should().Equal(new[] { "abc", "def" });
            ParseCodeBlocks(@"a` b `c").Should().Equal(new[] { " b " });
            ParseCodeBlocks(@"`abc`").Should().Equal(new[] { "abc" });
            ParseCodeBlocks("\n`abc`\n").Should().Equal(new[] { "abc" });

            ParseCodeBlocks(@"a ``abc`").Should().Equal(new[] { "" });
            ParseCodeBlocks(@"a ``v").Should().Equal(new[] { "" });
            ParseCodeBlocks(@"a \`v`").Should().BeEmpty();
        }

        [TestMethod]
        public void TestIndentedCodeBlocks()
        {
            ParseCodeBlocks("Hi there!\r\n\r\n    abc\r\nBye!").Should().Equal(new[] { "abc" });
        }
        [TestMethod]
        public void TestIndentedCodeBlocks_EmptyLinesBecomeEmptyArtifacts()
        {

            ParseCodeBlocks(@"Hi there!

    
Bye!").Should().Equal(new[] { "" }, "Empty lines become empty artifacts");
        }
        [TestMethod]
        public void TestIndentedCodeBlocks_UnlimitedWhitesapceIsAllowedInTheBlogBoundarayLine()
        {
            ParseCodeBlocks(@"
Three lines of four spaces each (boundary, then code):
    
    
Bye!").Should().Equal(new[] { "" }, "Unlimited whitespace is allowed in the block boundary line");
        }
        [TestMethod]
        public void TestIndentedCodeBlocks_WhitespaceOnlineCodeIsReported()
        {
            ParseCodeBlocks(@"
Five spaces, no other content:

     
Five spaces, surrounded by content:

    a
     
    b
Bye!").Should().Equal(new[] { " ", "a", " ", "b" }, "Whitespace-only code is reported");

        }
        [TestMethod]
        public void TestIndentedCodeBlocks_LeadingBlankLinesWillBeIgnored()
        {
            ParseCodeBlocks(@"Hi there!

    abc
    def
Bye!").Should().Equal(new[] { "abc", "def" });
        }
        //            ParseCodeBlocks(@"Hi there!
        // 1. List!
        //
        //        abc
        // * List!
        //
        //        def
        //
        // - More
        //    Not code!
        //Bye!").Should().Equal(new[] { "abc", "def" });
        //ParseCodeBlocks(" 1. abc\n\n  \t  Code!").Should().Equal(new[] { "Code!" });

        [TestMethod]
        public void TestIndentedCodeBlocks_TwoLines()
        {

            ParseCodeBlocks("Hi there!\n\n\tabc\n\tdef\nBye!").Should().Equal(new[] { "abc", "def" });
        }
        [TestMethod]
        public void TestIndentedCodeBlocks_()
        {
            ParseCodeBlocks(@"Hi there!
    abc
Bye!").Should().BeEmpty();
        }
        [TestMethod]
        public void TestIndentedCodeBlocks_BlankLineBeforeAndAfter()
        {
            ParseCodeBlocks(@"
    abc
").Should().Equal(new[] { "abc" });
        }
        [TestMethod]
        public void TestIndentedCodeBlocks_BlankLinkBeforNoLineAfter()
        {
            ParseCodeBlocks(@"
    abc").Should().Equal(new[] { "abc" });
        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks_UnquotedBlankLinkCountsAsBlockBoundary()
        {
            ParseCodeBlocks(@"Hi there!

>     abc
Bye!").Should().Equal(new[] { "abc" }, "Unquoted blank line counts as block boundary");
        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks_QuotedBlankLineCountsAsBlockBoundary()
        {
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>
>     abc
Bye!").Should().Equal(new[] { "abc" }, "Quoted blank line counts as block boundary");

            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>
>>>     abc
     def
>     ghi
Bye!").Should().Equal(new[] { "abc", " def", "ghi" }, "Quoted blank line counts as block boundary");
        }


        [TestMethod]
        public void TestQuotedIndentedCodeBlocks_InQuoteWithOnBlankLine()
        {
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!

>>>>     abc
Bye!").Should().Equal(new[] { "abc" });
        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks_MissingBlankLine()
        {
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>     > abc
Bye!").Should().BeEmpty("Missing blank line");
        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks_DeeperIdentCountsAsBlockBoundary()
        {
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>>     > abc
Bye!").Should().Equal(new[] { "> abc" }, "Deeper indent counts as block boundary");
        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks_LessDeppIdentStillCounts()
        {
            ParseCodeBlocks(@"Hi there!
>>>>
>     abc
Bye!").Should().Equal(new[] { "abc" }, "Less-deep indent still counts");

        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks_SimpleQuote()
        {
            ParseCodeBlocks(@"
>     abc").Should().Equal(new[] { "abc" });
        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks()
        {
            ParseCodeBlocks(@">     abc").Should().Equal(new[] { "abc" });

            // A code block in a quote needs five spaces, including one trailing space consumed by the quote.
            ParseCodeBlocks("\t> abc\n\t>def").Should().Equal(new[] { "> abc", ">def" }, "a tab at the beginning of the line is parsed as an indented code block, not a quote");
            ParseCodeBlocks(" >    > \tabc").Should().Equal(new[] { "abc" }, "up to four spaces are allowed between quote arrows");
            ParseCodeBlocks(" >\t> \tabc").Should().Equal(new[] { "abc" }, "a single tab is also allowed between quote arrows");
            ParseCodeBlocks(" >\t > abc").Should().Equal(new[] { "> abc" }, "a tab with a space together make up the five spaces for an indented code block");
            ParseCodeBlocks(" > \t> abc").Should().Equal(new[] { "> abc" }, "a partially-consumed tab should not be emitted as part of the code block");
            ParseCodeBlocks(" >  \t> abc").Should().Equal(new[] { "> abc" }, "a partially-consumed tab should not be emitted as part of the code block");
            ParseCodeBlocks(" >   \t > abc").Should().Equal(new[] { " > abc" }, "a partially-consumed tab should not affect subsequent spaces");
            ParseCodeBlocks(" >    \t > abc").Should().Equal(new[] { " > abc" }, "a partially-consumed tab should not be affect subsequent spaces");
            ParseCodeBlocks(" >     \t > abc").Should().Equal(new[] { "\t > abc" }, "five spaces should not affect subsequent tabs");
        }


        [TestMethod]
        public void TestFencedCodeBlocks_OneLine()
        {

            ParseCodeBlocks("Hi there!\r\n```\r\nabc\r\n```\r\nBye!").Should().Equal(new[] { "abc" }, "with CRLF");
            ParseCodeBlocks("Hi there!\n```\nabc\n```\nBye!").Should().Equal(new[] { "abc" }, "with LF");
        }

        [TestMethod]
        public void TestFencedCodeBlocks_TwoLines()
        {

            ParseCodeBlocks("Hi there!\r\n```\r\nabc\r\ndef\r\n```\r\nBye!").Should().Equal(new[] { "abc", "def" }, "with CRLF");
            ParseCodeBlocks("Hi there!\n```\nabc\ndef\n```\nBye!").Should().Equal(new[] { "abc", "def" }, "with LF");
            ParseCodeBlocks("Hi there!\n```\r\nabc\rdef\n```\nBye!").Should().Equal(new[] { "abc", "def" }, "With mixed line endings");
        }

        [TestMethod]
        public void TestFencedCodeBlocks_EmptyLinesBecomeEmptyArtifacts()
        {

            ParseCodeBlocks("Hi there!\r\n~~~\r\n\r\n~~~\r\nBye!").Should().Equal(new[] { "" }, "with CRLF");
            ParseCodeBlocks("Hi there!\n~~~\n\n~~~\nBye!").Should().Equal(new[] { "" }, "with LF");
        }

        [TestMethod]
        public void TestFencedCodeBlocks_NoArtifactsAreCreatedIfThereAreNoBlocks()
        {
            ParseCodeBlocks("Hi there!\r\n\r\n~~~\r\n~~~\r\nBye!").Should().BeEmpty("with CRLF");
            ParseCodeBlocks("Hi there!\n\n~~~\n~~~\nBye!").Should().BeEmpty("with LF");
            ParseCodeBlocks("Hi there!\n\n~~~\n~~~\r\nBye!").Should().BeEmpty("with mixed Lineendings");
        }

        [TestMethod]
        public void TestFencedCodeBlocks_TrailingBlankLinesDoNotBreakAnything()
        {
            ParseCodeBlocks("Hi there!\n\n~~~\nabc\ndef\n~~~\n\nBye!").Should().Equal(new[] { "abc", "def" }, "with LF");
            ParseCodeBlocks("Hi there!\r\n\r\n~~~\r\nabc\r\ndef\r\n~~~\r\n\r\nBye!").Should().Equal(new[] { "abc", "def" }, "with CRLF");
        }

        [TestMethod]
        public void TestFencedCodeBlocks_AlternateFencesAndBlankLinesAreHandledCorrectly()
        {
            ParseCodeBlocks("Hi there!\r\n\r\n```\r\nabc\r\n\r\n\r\n~~~\r\n```\r\nBye!").Should().Equal(new[] { "abc", "", "", "~~~" }, "with CRLF");
            ParseCodeBlocks("Hi there!\n\n```\nabc\n\n\n~~~\n```\nBye!").Should().Equal(new[] { "abc", "", "", "~~~" }, "with LF");

        }

        [TestMethod]
        public void TestFencedCodeBlocks_LeadingWhitespaceIsPreserverd()
        {


            ParseCodeBlocks(@"Hi there!
```
    abc
```
Bye!").Should().Equal(new[] { "    abc" }, "Leading whitespace is preserved");
        }

        [TestMethod]
        public void TestFencedCodeBlocks_ClosingFenceCannotHaveContentFollowing()
        {


            ParseCodeBlocks(@"Hi there!
```
abc
```   123
```
Bye!").Should().Equal(new[] { "abc", "```   123" }, "Closing fence cannot have content following");
        }

        [TestMethod]
        public void TestFencedCodeBlocks_ClosingFenceCanHaveUnlimitedWhitespaceFollowing()
        {

            ParseCodeBlocks(@"Hi there!
```
abc
```        
```
Bye!").Should().Equal(new[] { "abc", "Bye!" }, "Closing fence can have unlimited whitespace following");
        }

        [TestMethod]
        public void TestFencedCodeBlocks_LackOfSurroudingCharsDoesnotBreakAnything()
        {


            ParseCodeBlocks(@"```
abc
```").Should().Equal(new[] { "abc" }, "Lack of surrounding characters doesn't break anything");
        }
        [TestMethod]
        public void TestFencedCodeBlocks_EndingFencesIsOptional()
        {


            ParseCodeBlocks(@"```
abc").Should().Equal(new[] { "abc" }, "Ending fence is optional");
        }
        [TestMethod]
        public void TestFencedCodeBlocks_TrailingBlankLineWithoutFenceIsReported()
        {

            ParseCodeBlocks(@"```
abc
").Should().Equal(new[] { "abc", "" }, "Trailing blank line without fence is reported");
        }

        [TestMethod]
        public void TestQuotedFencedCodeBlocks()
        {
            ParseCodeBlocks(@"Hi there!
> ```
> abc
```
Bye!").Should().Equal(new[] { "abc" }, "Unquoted fence counts as block boundary");
            ParseCodeBlocks(@"Hi there!
>>>    ```
>>>   abc
  ```
Bye!").Should().Equal(new[] { "  abc" }, "Spaces before block boundary are ignored");
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>```
>     abc
>>>>```
Bye!").Should().Equal(new[] { "    abc" }, "Quoted fence counts as block boundary");

            // TODO: GitHub counts this as a quote; not sure why.
            // Apparently, they treat it as wrapping instead of a terminator.
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
```
>>>>abc
```
Bye!").Should().Equal(new[] { ">>>>abc" });
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>```
>>>>>     > abc
>>>>```
Bye!").Should().Equal(new[] { ">     > abc" }, "Deeper indent doesn't count as block boundary");
            ParseCodeBlocks(@"Hi there!
>>>>```
>     abc
```
Bye!").Should().Equal(new[] { "    abc" }, "Less-deep indent still counts");

            ParseCodeBlocks(@">```
>abc
```").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@">```
>abc").Should().Equal(new[] { "abc" });
        }
    }
}
