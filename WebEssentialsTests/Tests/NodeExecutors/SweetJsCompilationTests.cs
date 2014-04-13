using System;
using System.IO;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions;
using MadsKristensen.EditorExtensions.Settings;
using MadsKristensen.EditorExtensions.SweetJs;
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

                var expectedLines = await FileHelpers.ReadAllTextRetry(compiledFile);

                var compiledCode = await new SweetJsCompiler().CompileToStringAsync(sweetFileName);

                compiledCode.Replace("\r\n", "\n").Equals(expectedLines, StringComparison.CurrentCulture);
            }
        }
    }
}
