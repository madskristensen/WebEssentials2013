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
        static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);
        [TestMethod]
        public async Task PathCompilationTest()
        {
            foreach (var lessFilename in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures/less"), "*.less", SearchOption.AllDirectories))
            {
                var compiled = await CompileLess(lessFilename);
                var expected = File.ReadAllText(Path.ChangeExtension(lessFilename, ".css"));

                compiled = new CssFormatter().Format(compiled);
                expected = new CssFormatter().Format(expected);

                Assert.AreEqual(expected, compiled);
            }
        }


        static async Task<string> CompileLess(string fileName)
        {
            var result = await LessCompiler.Compile(fileName);
            if (result.IsSuccess)
                return result.Result;
            else
                throw new ExternalException(result.Error.Message);
        }
    }
}
