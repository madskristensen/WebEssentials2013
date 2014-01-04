using FluentAssertions;
using MadsKristensen.EditorExtensions.Helpers;
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

            stream.TryConsumeWhiteSpace(4).Should().BeTrue();
            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhiteSpace(1).Should().BeFalse();
        }

        [TestMethod]
        public void TestConsumeTab()
        {
            var stream = new TabAwareCharacterStream("a\tb");
            stream.CurrentChar.Should().Be('a');
            stream.MoveToNextChar();

            stream.TryConsumeWhiteSpace(4).Should().BeTrue();
            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhiteSpace(1).Should().BeFalse();
        }

        [TestMethod]
        public void TestConsumePartialTab()
        {
            var stream = new TabAwareCharacterStream("a\tb");
            stream.CurrentChar.Should().Be('a');
            stream.MoveToNextChar();

            stream.TryConsumeWhiteSpace(2).Should().BeTrue();
            stream.CurrentChar.Should().Be('b', "stream should be at next character after consuming partial tab");
            stream.TryConsumeWhiteSpace(2).Should().BeTrue();
            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhiteSpace(1).Should().BeFalse();
        }

        [TestMethod]
        public void TestPeekPartialTab()
        {
            var stream = new TabAwareCharacterStream("a\tb");
            stream.MoveToNextChar();

            stream.TryConsumeWhiteSpace(2).Should().BeTrue();

            using (stream.Peek())
                stream.TryConsumeWhiteSpace(2).Should().BeTrue();
            stream.TryConsumeWhiteSpace(2).Should().BeTrue("resetting peek should preserve partial tabs");

            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhiteSpace(1).Should().BeFalse();
        }

        [TestMethod]
        public void TestConsumeTooMuch()
        {
            var stream = new TabAwareCharacterStream("a\tb");
            stream.MoveToNextChar();

            stream.TryConsumeWhiteSpace(2).Should().BeTrue();

            stream.TryConsumeWhiteSpace(3).Should().BeFalse();
            stream.CurrentChar.Should().Be('b');
            stream.TryConsumeWhiteSpace(2).Should().BeTrue("consuming too much space should have no effect");
            stream.CurrentChar.Should().Be('b');

            stream.TryConsumeWhiteSpace(1).Should().BeFalse();
        }

        [TestMethod]
        public void TestConsumeMixedWhitespace()
        {
            var stream = new TabAwareCharacterStream("a  \t  \t\t \tb");

            stream.MoveToNextChar();
            // Stream is at first space

            stream.TryConsumeWhiteSpace(99).Should().BeFalse();
            stream.TryConsumeWhiteSpace(2).Should().BeTrue();
            // Stream is at center of first tab

            stream.TryConsumeWhiteSpace(99).Should().BeFalse();
            stream.TryConsumeWhiteSpace(4).Should().BeTrue();
            // Stream is at second tab

            stream.TryConsumeWhiteSpace(99).Should().BeFalse();
            stream.TryConsumeWhiteSpace(5).Should().BeTrue();
            // Stream is at first space in third tab

            stream.TryConsumeWhiteSpace(99).Should().BeFalse();
            stream.TryConsumeWhiteSpace(9).Should().BeTrue();
            stream.CurrentChar.Should().Be('b');
        }
    }
}
