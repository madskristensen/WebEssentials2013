using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class LessCompilationTests
    {
        private static string originalPath;
        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);
        private static readonly Regex _endingCurlyBraces = new Regex(@"}\W*}|}", RegexOptions.Compiled);
        private static readonly Regex _linesStartingWithTwoSpaces = new Regex("(\n( *))", RegexOptions.Compiled);

        #region Helper Methods
        private static async Task<string> CompileLess(string fileName, string targetFileName)
        {
            var result = await new LessCompiler().Compile(fileName, targetFileName);

            if (result.IsSuccess)
            {
                // Insert extra line-breaks between adjecent rule (to mimic the compiler's post-processing)
                File.WriteAllText(targetFileName, _endingCurlyBraces.Replace(_linesStartingWithTwoSpaces.Replace(File.ReadAllText(targetFileName).Trim(), "$1$2"), "$&\n"));

                return result.Result;
            }
            else
            {
                throw new ExternalException(result.Errors.First().Message);
            }
        }
        #endregion

        [ClassInitialize]
        public static void ObscureNode(TestContext context)
        {
            NodeExecutorBase.InUnitTests = true;
            originalPath = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", originalPath.Replace(@";C:\Program Files\nodejs\", ""), EnvironmentVariableTarget.Process);
        }

        [ClassCleanup]
        public static void RestoreNode()
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }

        [TestMethod]
        public async Task PathCompilationTest()
        {
            var sourcePath = Path.Combine(BaseDirectory, "fixtures\\less");
            foreach (var lessFilename in Directory.EnumerateFiles(sourcePath, "*.less", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(lessFilename, ".css");
                var compiled = await CompileLess(lessFilename, compiledFile);
                var expected = File.ReadAllText(compiledFile)
                               .Replace("\r", "");

                compiled.Should().Be(expected.Trim());
            }
        }

        [TestMethod]
        public async Task PathNormalizationTest()
        {
            foreach (var lessFilename in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures\\less"), "*.less", SearchOption.AllDirectories))
            {
                var expectedPath = Path.Combine(Path.GetDirectoryName(lessFilename), "css", Path.ChangeExtension(lessFilename, ".css"));

                if (!File.Exists(expectedPath))
                    continue;

                var compiled = await CompileLess(lessFilename, expectedPath);
                var expected = File.ReadAllText(expectedPath);

                compiled = new CssFormatter().Format(compiled).Replace("\r", "");
                expected = new CssFormatter().Format(expected).Replace("\r", "");

                compiled.Should().Be(expected);
            }
        }
    }
}
