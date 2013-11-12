using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class LessCompilationTests
    {
        static string originalPath;
        [ClassInitialize]
        public static void ObscureNode(TestContext context)
        {
            originalPath = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", originalPath.Replace(@";C:\Program Files\nodejs\", ""), EnvironmentVariableTarget.Process);
        }
        [ClassCleanup]
        public static void RestoreNode()
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }

        static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);
        [TestMethod]
        public async Task PathCompilationTest()
        {
            foreach (var lessFilename in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures/less"), "*.less", SearchOption.AllDirectories))
            {
                var compiled = await CompileLess(lessFilename);
                var expected = File.ReadAllText(Path.ChangeExtension(lessFilename, ".css"));

                compiled = new CssFormatter().Format(compiled).Replace("\r", "");
                expected = new CssFormatter().Format(expected).Replace("\r", "");

                Assert.AreEqual(expected, compiled);
            }
        }

        [TestMethod]
        public async Task PathNormalizationTest()
        {
            foreach (var lessFilename in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures/less"), "*.less", SearchOption.AllDirectories))
            {
                var expectedPath = Path.Combine(Path.GetDirectoryName(lessFilename), "css", Path.GetFileNameWithoutExtension(lessFilename) + ".css");
                if (!File.Exists(expectedPath))
                    continue;

                var compiled = await CompileLess(lessFilename, expectedPath);
                var expected = File.ReadAllText(expectedPath);

                compiled = new CssFormatter().Format(compiled).Replace("\r", "");
                expected = new CssFormatter().Format(expected).Replace("\r", "");

                Assert.AreEqual(expected, compiled);
            }
        }


        static async Task<string> CompileLess(string fileName, string targetFilename = null)
        {
            var result = await LessCompiler.Compile(fileName, targetFilename);
            if (result.IsSuccess)
                return result.Result;
            else
                throw new ExternalException(result.Error.Message);
        }
    }
}
