using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions.Scss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class ScssCompilationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                Scss = { GenerateSourceMaps = false }
            });
        }

        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(ScssCompilationTests).Assembly.Location);

        [TestMethod]
        public async Task ScssBasicCompilationTest()
        {
            foreach (var sourceFile in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "scss"), "*.scss", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(sourceFile, ".css");

                if (!File.Exists(compiledFile))
                    continue;

                var expected = File.ReadAllText(compiledFile)
                                   .Replace("\r", "");
                var compiled = await new ScssCompiler().CompileToStringAsync(sourceFile);

                compiled.Trim().Should().Be(expected.Trim());
            }
        }
    }
}
