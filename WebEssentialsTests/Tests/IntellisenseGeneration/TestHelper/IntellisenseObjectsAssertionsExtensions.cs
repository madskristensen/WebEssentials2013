using MadsKristensen.EditorExtensions;

namespace WebEssentialsTests.Tests.IntellisenseGeneration.TestHelper
{
    public static class IntellisenseObjectsAssertionsExtensions
    {
        public static IntellisenseObjectsAssertions Should(this IntellisenseObject @object)
        {
            return new IntellisenseObjectsAssertions(@object);
        }
    }
}