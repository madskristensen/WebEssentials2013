using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.Web.Core;

namespace WebEssentialsTests
{
    [TestClass]
    public class MarkdownParserTests
    {
        static List<string> ParseCodeBlocks(string markdown)
        {
            var retVal = new List<string>();
            var parser = new MarkdownParser(new CharacterStream(markdown));
            parser.ArtifactFound += (s, e) => retVal.Add(markdown.Substring(e.Artifact.InnerRange.Start, e.Artifact.InnerRange.Length));
            parser.Parse();
            return retVal;
        }

        [TestMethod]
        public void TestInlineCodeBlocks()
        {
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"Hi there! `abc` Bye!"));
            CollectionAssert.AreEquivalent(new[] { " b " }, ParseCodeBlocks(@"a` b `c"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks(@"`abc`"));
            CollectionAssert.AreEquivalent(new[] { "abc" }, ParseCodeBlocks("\n`abc`\n"));
        }
    }
}
