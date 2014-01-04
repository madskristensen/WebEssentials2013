using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using MadsKristensen.EditorExtensions;

namespace WebEssentialsTests.Tests.IntellisenseGeneration.TestHelper
{
    [DebuggerNonUserCode]
    public class IntellisenseObjectsAssertions :
        ReferenceTypeAssertions<IntellisenseObject, IntellisenseObjectsAssertions>
    {
        protected internal IntellisenseObjectsAssertions(IntellisenseObject @object)
        {
            Subject = @object;
        }

        protected override string Context
        {
            get { return "WebEssentials IntellisenseObject"; }
        }

        public AndConstraint<IntellisensePropertyAssertions> HasProperty(string propertyName)
        {
            return HasProperty(propertyName, string.Empty);
        }

        public AndConstraint<IntellisensePropertyAssertions> HasProperty(string propertyName, string reason,
            params object[] reasonArgs)
        {
            var property = Subject.Properties.SingleOrDefault(p => p.Name == propertyName);
            Execute.Assertion
                .ForCondition(property != null)
                .BecauseOf(reason, reasonArgs)
                .FailWith("Expected IntellisenseObject to have property {0}{reason}", propertyName);

            return new AndConstraint<IntellisensePropertyAssertions>(new IntellisensePropertyAssertions(property));
        }

        public AndConstraint<IntellisenseObjectsAssertions> NameIs(string expected)
        {
            return NameIs(expected, string.Empty);
        }

        public AndConstraint<IntellisenseObjectsAssertions> NameIs(string expected, string reason,
            params object[] reasonArgs)
        {
            Execute.Assertion.ForCondition(Subject.Name == expected)
                .BecauseOf(reason, reasonArgs)
                .FailWith("Expected IntellisenseObject to be named {0} but found {1}{reason}", expected, Subject.Name);

            return new AndConstraint<IntellisenseObjectsAssertions>(this);
        }
    }
}