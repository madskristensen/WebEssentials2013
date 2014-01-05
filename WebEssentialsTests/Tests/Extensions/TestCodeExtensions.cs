using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace WebEssentialsTests
{
    public static class TestCodeExtensions
    {
        private static AndConstraint<StringAssertions> BeCode(this StringAssertions assertion, string expectedCode)
        {
            var lines = TrimLines(assertion.Subject);
            var expectedLines = TrimLines(expectedCode);
            Execute.Assertion
                .ForCondition(lines.Length == expectedLines.Length)
                .FailWith(
                    "Expected Code \r\n{2} \r\nhas {0} lines but \r\n{3}\r\n has {1} lines, empty lines don't count",
                    expectedLines.Length,
                    lines.Length,
                    string.Join(Environment.NewLine, expectedLines),
                    string.Join(Environment.NewLine, lines)
                    );


            for (int i = 0; i < lines.Length; i++)
            {
                Execute.Assertion
                    .ForCondition(lines[i] == expectedLines[i])
                    .FailWith(
                        "Expected code in line {0} is \r\n{1}\r\n but was \r\n{2}\r\n, whitespaces and the begining and end are not relevant",
                        i + 1, expectedLines[i], lines[i]);
            }
            return new AndConstraint<StringAssertions>(assertion);
        }

        private static string[] TrimLines(string toString)
        {
            string[] lines = toString.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);

            return lines.Select(t => t.Trim()).Where(trimmedLine => !string.IsNullOrWhiteSpace(trimmedLine)).ToArray();
        }


        public static AndConstraint<StringAssertions> ShouldBeCode(this StringBuilder sb, string expectedCode)
        {
            return sb.ToString().Should().BeCode(expectedCode);
        }
    }
}