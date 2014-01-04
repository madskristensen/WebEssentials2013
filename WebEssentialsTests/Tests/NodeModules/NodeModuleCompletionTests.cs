using System;
using System.Linq;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;

namespace WebEssentialsTests
{
    [TestClass]
    public class NodeModuleCompletionTests
    {
        #region Helper Methods
        private static void TestCase(string input, string expectedBasePath = null)
        {
            var cursorIndex = input.IndexOf('|');
            if (cursorIndex < 0 || cursorIndex != input.LastIndexOf('|'))
                throw new ArgumentException("Test case must have exactly one | character for the cursor position.", "input");

            input = input.Remove(cursorIndex, 1);

            Tuple<string, Span> expected = null;

            var spanStart = input.IndexOf('<');
            if (spanStart < 0 && expectedBasePath != null)
                throw new ArgumentException("If no activation range is specified in the input, expectedPath should not be passed.", "expectedPath");
            if (spanStart >= 0)
            {
                if (spanStart <= cursorIndex)
                    cursorIndex--;  // Decrement the cursor to allow for the removed character

                input = input.Remove(spanStart, 1);

                int spanEnd = input.IndexOf('>');
                if (spanEnd < spanStart || spanEnd != input.LastIndexOf('>') || input.Contains('<'))
                    throw new ArgumentException("Test case must have at most one range enclosed in <angle brackets> for the expected IntelliSense activation range.", "input");

                if (spanEnd < cursorIndex)
                    cursorIndex--;  // Decrement the cursor to allow for the removed character

                input = input.Remove(spanEnd, 1);

                expected = Tuple.Create(expectedBasePath, Span.FromBounds(spanStart, spanEnd));
            }
            NodeModuleCompletionUtils.FindCompletionInfo(input, cursorIndex).Should().Be(expected);
        }
        #endregion

        [TestMethod]
        public void TestNonActivation()
        {
            TestCase(@"require(['a|', 'b'], ...");
            TestCase(@"requireAsync('a|', ...");
            TestCase(@"myrequire('a|', ...");
            TestCase(@"require('a'|, ...");
            TestCase(@"require(|'a', ...");

            TestCase(@"requ|ire('module1/'), require('module2/')");
            TestCase(@"require('module1/'|), require('module2/')");
            TestCase(@"require('module1/'), req|uire('module2/')");
            TestCase(@"require('module1/'), require('module2/'|)");
        }

        [TestMethod]
        public void TestBasicActivation()
        {
            TestCase(@"require('<a|>', ...", null);
            TestCase(@"require('<a|>", null);
            TestCase(@"require('<a|bbbbbbbbbb>", null);
            TestCase(@"require('<|a>', ...", null);
            TestCase(@"require('<|a>', ...", null);

            TestCase(@"require ( '<|a>', ...", null);

            TestCase(@"require(""<|>"", ...", null);
            TestCase(@"require('<|>', ...", null);
            TestCase(@"require('<|>, ...", null);

            TestCase(@"require('module1/'), require('module2/<|>')", "module2/");

            TestCase(@"require('<a|b>', ...", null);
            // Stop at comma if no trailing quote
            TestCase(@"require('<ab|>, ...", null);
            TestCase(@"require('<a|b>, ...", null);
            TestCase(@"require('<|>, ...", null);
        }

        [TestMethod]
        public void TestPrefixPath()
        {

            TestCase(@"require('<|myModule>/', ...", null);
            TestCase(@"require('<myModule|>/', ...", null);
            TestCase(@"require('<myModule|>', ...", null);

            TestCase(@"require('myModule/<|>', ...", "myModule/");
            TestCase(@"require('myModule/<a|b>', ...", "myModule/");
            TestCase(@"require('myModule/<|ab>', ...", "myModule/");

            TestCase(@"require('../<myFolder|>/ab', ...", "../");
            TestCase(@"require('../myFolder/<a|b>', ...", "../myFolder/");

            TestCase(@"require('myModule/subDir/<a|b>/deepr/file.js', ...", "myModule/subDir/");
        }
    }
}
