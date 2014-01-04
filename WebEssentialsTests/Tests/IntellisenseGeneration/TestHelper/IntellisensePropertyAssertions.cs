using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using MadsKristensen.EditorExtensions;

namespace WebEssentialsTests.Tests.IntellisenseGeneration.TestHelper
{
    public class IntellisensePropertyAssertions :
        ReferenceTypeAssertions<IntellisenseProperty, IntellisensePropertyAssertions>
    {
        protected internal IntellisensePropertyAssertions(IntellisenseProperty @object)
        {
            Subject = @object;
        }

        protected override string Context
        {
            get { return "WebEssentials IntellisenseProperty"; }
        }

        public AndConstraint<IntellisensePropertyAssertions> IsArray(string reason, params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(Subject.Type.IsArray)
                .BecauseOf(reason, reasonArgs)
                .FailWith("Expected {0} to be an array{reason}", Subject.Name);
            return new AndConstraint<IntellisensePropertyAssertions>(this);
        }

        public AndConstraint<IntellisensePropertyAssertions> IsNotAnArray(string reason, params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(!Subject.Type.IsArray)
                .BecauseOf(reason, reasonArgs)
                .FailWith("Expected {0} not to be an array{reason}", Subject.Name);
            return new AndConstraint<IntellisensePropertyAssertions>(this);
        }

        public AndConstraint<IntellisensePropertyAssertions> JavaScriptNameIs(string expected, string reason,
            params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(Subject.Type.JavaScriptName == expected)
                .BecauseOf(reason, reasonArgs)
                .FailWith("Expected {0} to have JavaScriptName {1}{reason}, but found {2}", Subject.Name, expected, Subject.Type.JavaScriptName);
            return new AndConstraint<IntellisensePropertyAssertions>(this);
        }

        public AndConstraint<IntellisensePropertyAssertions> TypeScriptNameIs(string expected, string reason,
            params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(Subject.Type.TypeScriptName == expected)
                .BecauseOf(reason, reasonArgs)
                .FailWith("Expected {0} to have TypeScriptName {1}{reason}, but found {2}", Subject.Name, expected, Subject.Type.TypeScriptName);
            return new AndConstraint<IntellisensePropertyAssertions>(this);
        }

        public AndConstraint<IntellisensePropertyAssertions> IsArray()
        {
            return IsArray(string.Empty);
        }

        public AndConstraint<IntellisensePropertyAssertions> IsNotAnArray()
        {
            return IsNotAnArray(string.Empty);
        }
        public AndConstraint<IntellisensePropertyAssertions> JavaScriptNameIs(string expected)
        {
            return JavaScriptNameIs(expected, string.Empty);
        }

        public AndConstraint<IntellisensePropertyAssertions> TypeScriptNameIs(string expected)
        {
            return TypeScriptNameIs(expected, string.Empty);
        }
    }
}
