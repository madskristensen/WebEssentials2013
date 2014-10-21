using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions.Scss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class RubyScssCompilationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                Scss =
                {
                    GenerateSourceMaps = false,
                    UseRubyRuntime = true
                }
            });
        }

        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(ScssCompilationTests).Assembly.Location);

        [TestMethod]
        public async Task RubyScssBasicCompilationTest()
        {
            foreach (var sourceFile in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "ruby-scss"), "*.scss", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(sourceFile, ".css");

                var expected = File.ReadAllText(compiledFile).Replace("\r", "").Replace("\n", "").Replace(" ", "");
                var compiled = await new ScssCompiler().CompileToStringAsync(sourceFile);

                compiled.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim().Should().Be(expected.Trim());
            }
        }
    }
}
