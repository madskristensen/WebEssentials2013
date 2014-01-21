using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class SassCompilationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                Sass = { GenerateSourceMaps = false }
            });
        }


        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(SassCompilationTests).Assembly.Location);

        [TestMethod]
        public async Task SassBasicCompilationTest()
        {
            foreach (var sourceFile in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "sass"), "*.scss", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(sourceFile, ".css");

                if (!File.Exists(compiledFile))
                    continue;

                var expected = File.ReadAllText(compiledFile)
                                   .Replace("\r", "");
                var compiled = await new SassCompiler().CompileToStringAsync(sourceFile);

                compiled.Should().Be(expected);
            }
        }
    }
}
