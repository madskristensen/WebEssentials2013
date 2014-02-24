using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.Tests.NodeExecutors
{
    [TestClass]
    public class SweetJsCompilationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                SweetJs = { GenerateSourceMaps = false }
            });
        }

        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);

        [TestMethod]
        public async Task SweetJsBasicCompilationTest()
        {
            foreach (var sweetFileName in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "sweet.js", "source"), "*.sjs", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(sweetFileName, ".js").Replace("source\\", "compiled\\");

                if (!File.Exists(compiledFile))
                    continue;

                var expectedLines = File.ReadLines(compiledFile);

                var compiledCode = await new SweetJsCompiler().CompileToStringAsync(sweetFileName);

                compiledCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Should().Equal(expectedLines);
            }
        }
    }
}
