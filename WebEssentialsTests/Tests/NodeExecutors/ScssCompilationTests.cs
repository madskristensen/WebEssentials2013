using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions.Scss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

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
        private static readonly Regex BlockComments = new Regex(@"/\*(.*?)\*/", RegexOptions.IgnoreCase);


        [TestMethod]
        public async Task ScssBasicCompilationTest()
        {
            foreach (var sourceFile in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "scss"), "*.scss", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(sourceFile, ".css");

                if (!File.Exists(compiledFile))
                    continue;

                var expected = CleanCss(File.ReadAllText(compiledFile));
                var compiled = await new ScssCompiler().CompileToStringAsync(sourceFile);

                CleanCss(compiled).Should().Be(expected);
            }
        }

        private static string CleanCss(string css)
        {
            css = css.Replace("\n", "").Replace("\r", "").Replace(" ", "");
            css = BlockComments.Replace(css, "");

            return css.Trim();
        }
    }
}
