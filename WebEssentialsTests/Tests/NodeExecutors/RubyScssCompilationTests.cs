using System.IO;
using System.Text.RegularExpressions;
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
        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(ScssCompilationTests).Assembly.Location);
        private static readonly Regex BlockComments = new Regex(@"/\*(.*?)\*/", RegexOptions.IgnoreCase);

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

        [TestMethod]
        public async Task RubyScssBasicCompilationTest()
        {
            foreach (var sourceFile in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "ruby-scss", "source"), "*.scss"))
            {
                var compiledFile = Path.ChangeExtension(sourceFile, ".css").Replace("source\\", "compiled\\");

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
