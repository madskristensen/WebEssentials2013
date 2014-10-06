using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions.CoffeeScript;
using MadsKristensen.EditorExtensions.Handlebars;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.Tests.Handlebars
{
    [TestClass]
    public class HandlebarsCompilationTests
    {
        //private static string originalPath;

        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                Handlebars = { MinifyInPlace = false }
            });
            //originalPath = Environment.GetEnvironmentVariable("PATH");
            //Environment.SetEnvironmentVariable("PATH", originalPath.Replace(@";C:\Program Files\nodejs\", ""), EnvironmentVariableTarget.Process);
        }

        //[ClassCleanup]
        //public static void RestoreNode()
        //{
        //    Environment.SetEnvironmentVariable("PATH", originalPath);
        //}

        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);

        [TestMethod]
        public async Task HandlebarsBasicCompilationTest()
        {
            foreach (var handlebarsFileName in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures", "handlebars", "source"), "*.hbs", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(handlebarsFileName, ".hbs.js").Replace("source\\", "compiled\\");

                if (!File.Exists(compiledFile))
                    continue;

                var expectedCompiledCode = File.ReadAllText(compiledFile);

                var compiledCode = await new HandlebarsCompiler().CompileToStringAsync(handlebarsFileName);


                compiledCode.Trim().Should().Be(expectedCompiledCode.Trim());
            }
        }
    }
}
