using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MadsKristensen.EditorExtensions.Compilers.Sweet.js;

namespace WebEssentialsTests.Tests.NodeExecutors
{
    [TestClass]
    public class SweetjsCompilationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                Sweetjs = { GenerateSourceMaps = false }
            });
        }

        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);

        [TestMethod]
        public async Task SweetjsBasicCompilationTest()
        {
            foreach (var sweetFileName in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "sweet.js", "source"), "*.sjs", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(sweetFileName, ".js").Replace("source\\", "compiled\\");

                if (!File.Exists(compiledFile))
                    continue;

                var expectedLines = File.ReadLines(compiledFile);

                var compiledCode = await new SweetjsCompiler().CompileToStringAsync(sweetFileName);

                compiledCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Should().Equal(expectedLines);
            }
        }
    }
}
