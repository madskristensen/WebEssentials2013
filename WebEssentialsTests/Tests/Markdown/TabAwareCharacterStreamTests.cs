using System;
using FluentAssertions;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.Tests.Markdown
{
    [TestClass]
    public class TabAwareCharacterStreamTests
    {
        [TestMethod]
        public void TestConsumeSpaces()
        {
            var stream = new TabAwareCharacterStream("a    b");
            stream.CurrentChar.Should().Be('a');
            stream.MoveToNextChar();

            stream.TryConsumeWhitespace(4).Should().BeTrue();
            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhitespace(1).Should().BeFalse();
        }
        [TestMethod]
        public void TestConsumeTab()
        {
            var stream = new TabAwareCharacterStream("a\tb");
            stream.CurrentChar.Should().Be('a');
            stream.MoveToNextChar();

            stream.TryConsumeWhitespace(4).Should().BeTrue();
            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhitespace(1).Should().BeFalse();
        }
        [TestMethod]
        public void TestConsumePartialTab()
        {
            var stream = new TabAwareCharacterStream("a\tb");
            stream.CurrentChar.Should().Be('a');
            stream.MoveToNextChar();

            stream.TryConsumeWhitespace(2).Should().BeTrue();
            stream.CurrentChar.Should().Be('b', "stream should be at next character after consuming partial tab");
            stream.TryConsumeWhitespace(2).Should().BeTrue();
            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhitespace(1).Should().BeFalse();
        }
        [TestMethod]
        public void TestPeekPartialTab()
        {
            var stream = new TabAwareCharacterStream("a\tb");
            stream.MoveToNextChar();

            stream.TryConsumeWhitespace(2).Should().BeTrue();

            using (stream.Peek())
                stream.TryConsumeWhitespace(2).Should().BeTrue();
            stream.TryConsumeWhitespace(2).Should().BeTrue("resetting peek should preserve partial tabs");

            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhitespace(1).Should().BeFalse();
        }
        [TestMethod]
        public void TestConsumeTooMuch()
        {
            var stream = new TabAwareCharacterStream("a\tb");
            stream.MoveToNextChar();

            stream.TryConsumeWhitespace(2).Should().BeTrue();

            stream.TryConsumeWhitespace(3).Should().BeFalse();
            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhitespace(2).Should().BeTrue("consuming too much space should have no effect");
            stream.CurrentChar.Should().Be('b');

            stream.TryConsumeWhitespace(1).Should().BeFalse();
        }
        [TestMethod]
        public void TestConsumeMixedWhitespace()
        {
            var stream = new TabAwareCharacterStream("a  \t  \t\t \tb");

            stream.MoveToNextChar();
            // Stream is at first space

            stream.TryConsumeWhitespace(99).Should().BeFalse();
            stream.TryConsumeWhitespace(2).Should().BeTrue();
            // Stream is at center of first tab

            stream.TryConsumeWhitespace(99).Should().BeFalse();
            stream.TryConsumeWhitespace(4).Should().BeTrue();
            // Stream is at second tab

            stream.TryConsumeWhitespace(99).Should().BeFalse();
            stream.TryConsumeWhitespace(5).Should().BeTrue();
            // Stream is at first space in third tab

            stream.TryConsumeWhitespace(99).Should().BeFalse();
            stream.TryConsumeWhitespace(9).Should().BeTrue();
            stream.CurrentChar.Should().Be('b');
        }
    }
}
