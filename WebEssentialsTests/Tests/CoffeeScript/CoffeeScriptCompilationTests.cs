using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class CoffeeScriptCompilationTests
    {
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
                var expectedCode = File.ReadAllText(compiledFile)
                               .Replace("\r", "");

                compiledCode.Should().Be(expectedCode);
            }
        }
    }
}
