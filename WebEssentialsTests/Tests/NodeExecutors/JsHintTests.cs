using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.Tests.JsHint
{
    [TestClass]
    public class JsHintTests
    {
        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(JsHintTests).Assembly.Location);
        private static readonly string SourcePath = Path.Combine(BaseDirectory, @"fixtures\jshint");

        [TestMethod]
        public async Task JsHintIgnoreTest()
        {
            var result = await new JsHintCompiler().CheckAsync(Path.Combine(SourcePath, "skip.js"));
            result.Errors.Should().BeEmpty();
        }

        [TestMethod]
        public async Task JsHintConfigTest()
        {
            var result = await new JsHintCompiler().CheckAsync(Path.Combine(SourcePath, "config", "clean.js"));
            result.Errors.Should().BeEmpty();
        }

        [TestMethod]
        public async Task JsHintErrorTest()
        {
            var result = await new JsHintCompiler().CheckAsync(Path.Combine(SourcePath, "default.js"));
            result.Errors.Select(e => e.Message.Substring(e.Message.IndexOf("): ") + 3))
                         .Should().BeEquivalentTo(new[] {
                            "'undef1' is not defined.",
                            "'undef2' is not defined.",
                            "'undef3' is not defined.",
                            "Expected '===' and instead saw '=='.",
                            "'unused' is defined but never used."
                        });
        }
    }
}
