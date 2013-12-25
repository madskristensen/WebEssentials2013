using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class IcedCoffeeScriptCompilationTests
    {
        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);

        [TestMethod]
        public async Task IcedBasicCompilationTest()
        {
            foreach (var icedFileName in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "iced", "source"), "*.iced", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(icedFileName, ".js").Replace("source\\", "compiled\\");

                if (!File.Exists(compiledFile))
                    continue;

                var compiledCode = await new CoffeeScriptCompiler().CompileString(File.ReadAllText(icedFileName), ".iced", ".js");
                var expectedCode = File.ReadAllText(compiledFile)
                               .Replace("\r", "");

                compiledCode.Should().Be(expectedCode);
            }
        }
    }
}
