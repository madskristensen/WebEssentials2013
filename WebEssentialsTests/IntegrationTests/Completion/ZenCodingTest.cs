using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using EnvDTE;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace WebEssentialsTests.IntegrationTests.Compilation
{
    [TestClass]
    public class ZenCodingTest
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
        public async Task HtmlZenCodingTest()
        {
            string result = await WriteZenCoding("#id.class", ".html");
            result.Should().Be("<div id=\"id\" class=\"class\"></div>");
        }

        [HostType("VS IDE")]
        //[TestMethod, TestCategory("Completion")]
        public async Task AspxZenCodingTest()
        {
            string result = await WriteZenCoding("#id.class", ".aspx");
            result.Should().Be("<div id=\"id\" class=\"class\"></div>");
        }

        private static async Task<string> WriteZenCoding(string text, string extension)
        {
            string fileName = CreateHtmlFile(extension);
            var window = _dte.ItemOperations.OpenFile(fileName);

            await VSHost.TypeString(text + "\t");
            await VSHost.Dispatcher.NextFrame(DispatcherPriority.ApplicationIdle);
            window.Document.Save();

            return File.ReadAllText(fileName);
        }

        private static string CreateHtmlFile(string extension)
        {
            var fileName = Path.Combine(_testDir, Guid.NewGuid() + extension);

            File.WriteAllText(fileName, string.Empty);
            return fileName;
        }
    }
}