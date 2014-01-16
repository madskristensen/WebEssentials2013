using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class IcedCoffeeScriptCompilationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c) { SettingsStore.EnterTestMode(); }

        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);

        [TestMethod]
        public async Task IcedBasicCompilationTest()
        {
            foreach (var icedFileName in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "iced", "source"), "*.iced", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(icedFileName, ".js").Replace("source\\", "compiled\\");

                if (!File.Exists(compiledFile))
                    continue;

                var compiledCode = await new IcedCoffeeScriptCompiler().CompileString(File.ReadAllText(icedFileName), ".iced", ".js");

                // Skip the version header, so we don't need
                // to update the expecation for CS releases.
                var compiledLines = compiledCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1);
                var expectedLines = File.ReadAllText(compiledFile).Replace("\r", "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1); ;

                compiledLines.Should().Equal(expectedLines);
            }
        }
    }
}
