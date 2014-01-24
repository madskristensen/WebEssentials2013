using System;
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
    public class CommentsTest
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
        public async Task BlockCommentCompletion()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = true }
            });
            string result = await WriteBlockComment();
            result.Should().Be("/**/");
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task BlockCommentCompletionDisabled()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = false }
            });

            string result = await WriteBlockComment();
            result.Should().Be("/*");
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task BlockCommentStarCompletion()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = true }
            });

            string result = await WriteBlockStarComment();
            result.Should().Be("/*\r\n * \r\n */");
        }

        [HostType("VS IDE")]
        [TestMethod, TestCategory("Completion")]
        public async Task BlockCommentStarCompletionDisabled()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = false }
            });

            string result = await WriteBlockStarComment();
            result.Should().Be("/*\r\n");
        }

        private static async Task<string> WriteBlockComment()
        {
            string fileName = CreateJavaScriptFile();
            var window = _dte.ItemOperations.OpenFile(fileName);
            await Task.Delay(1500);

            await VSHost.TypeString("/*");
            await Task.Delay(500);
            window.Document.Save();

            return File.ReadAllText(fileName);
        }



        private static async Task<string> WriteBlockStarComment()
        {
            var fileName = CreateJavaScriptFile();
            var window = _dte.ItemOperations.OpenFile(fileName);

            await VSHost.TypeString("/*\n");
            await Task.Delay(500);

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
