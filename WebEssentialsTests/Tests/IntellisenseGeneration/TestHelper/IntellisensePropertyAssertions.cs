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
            get { return "WebEssentials IntellisenseObject"; }
        }

        public AndConstraint<IntellisensePropertyAssertions> IsArray(string reason, params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(Subject.Type.IsArray)
                .BecauseOf(reason, reasonArgs)
                .FailWith("{0} is not an array", Subject.Name);
            return new AndConstraint<IntellisensePropertyAssertions>(this);
        }

        public AndConstraint<IntellisensePropertyAssertions> JavaScriptNameIs(string expected, string reason,
            params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(Subject.Type.JavaScriptName == expected)
                .BecauseOf(reason, reasonArgs)
                .FailWith("the JavaScriptName is {0} but should be {1}", Subject.Type.JavaScriptName, expected);
            return new AndConstraint<IntellisensePropertyAssertions>(this);
        }

        public AndConstraint<IntellisensePropertyAssertions> TypeScriptNameIs(string expected, string reason,
            params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(Subject.Type.TypeScriptName == expected)
                .BecauseOf(reason, reasonArgs)
                .FailWith("the TypeScriptName is {0} but should be {1}", Subject.Type.TypeScriptName, expected);
            return new AndConstraint<IntellisensePropertyAssertions>(this);
        }

        public AndConstraint<IntellisensePropertyAssertions> IsArray()
        {
            return IsArray(string.Empty);
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