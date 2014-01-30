using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using EnvDTE;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace WebEssentialsTests.IntegrationTests.Compilation
{
    [TestClass]
    public class JsIntelliSenseTest
    {
        private static DTE _dte { get { return VsIdeTestHostContext.Dte; } }
        private static string _testDir { get; set; }

        [HostType("VS IDE")]
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            _testDir = Path.Combine(Path.GetTempPath(), "Web Essentials Test Files", c.FullyQualifiedTestClassName + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            Directory.CreateDirectory(_testDir);
        }

        [ClassCleanup]
        public static void DeleteTestCase()
        {
            Directory.Delete(_testDir, true);
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task UseStrictEmptyFile()
        {
            string result = await TypeText("'u\t");
            result.Should().Be("'use strict';");
        }
        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task UseAsmFunction()
        {
            string result = await TypeText("var a=function(){\n\"use a\"");
            result.Should().EndWith("\"use asm\"\r\n}");
        }
        // TODO: Test that "use strict" completion doesn't trigger elsewhere, and that deleting the quote closes completion.
        // To do this, we need to check whether the completion window is open.

        // TODO: Test require() completion with fixtures, including nested paths

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task GetElementsByTagName()
        {
            string result = await TypeText("document.body.getElementsByTagName('ta')");
            result.Should().Be("document.body.getElementsByTagName('table')");
        }

        private static async Task<string> TypeText(string keystrokes)
        {
            var fileName = CreateJavaScriptFile();
            var window = _dte.ItemOperations.OpenFile(fileName);

            await VSHost.TypeString(keystrokes);

            window.Document.Save();

            return File.ReadAllText(fileName);
        }

        private static string CreateJavaScriptFile()
        {
            var fileName = Path.Combine(_testDir, Guid.NewGuid() + ".js");

            File.WriteAllText(fileName, string.Empty);
            return fileName;
        }
    }
}
