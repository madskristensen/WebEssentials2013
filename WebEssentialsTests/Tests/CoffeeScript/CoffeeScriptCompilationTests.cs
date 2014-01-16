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
    public class CoffeeScriptCompilationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext c) { SettingsStore.EnterTestMode(); }

        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);

        [TestMethod]
        public async Task CoffeeBasicCompilationTest()
        {
            foreach (var coffeeFileName in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "coffee", "source"), "*.coffee", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(coffeeFileName, ".js").Replace("source\\", "compiled\\");

                if (!File.Exists(compiledFile))
                    continue;

                var compiledCode = await new CoffeeScriptCompiler().CompileString(File.ReadAllText(coffeeFileName), ".coffee", ".js");

                // Skip the version header, so we don't need
                // to update the expecation for CS releases.
                var compiledLines = compiledCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1);
                var expectedLines = File.ReadLines(compiledFile).Skip(1).Where(s => !string.IsNullOrEmpty(s));

                compiledLines.Should().Equal(expectedLines);
            }
        }
    }
}
