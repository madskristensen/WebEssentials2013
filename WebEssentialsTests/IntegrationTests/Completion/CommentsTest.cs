using System;
using System.IO;
using System.Windows.Forms;
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
        [TestMethod]
        public void BlockCommentCompletion()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = true }
            });

            string result = WriteBlockComment();
            result.Should().Be("/**/");
        }

        [HostType("VS IDE")]
        [TestMethod]
        public void BlockCommentCompletionDisabled()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = false }
            });

            string result = WriteBlockComment();
            result.Should().Be("/*");
        }

        [HostType("VS IDE")]
        [TestMethod]
        public void BlockCommentStarCompletion()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = true }
            });

            string result = WriteBlockStarComment();
            result.Should().Be("/*\r\n * \r\n */");
        }

        [HostType("VS IDE")]
        [TestMethod]
        public void BlockCommentStarCompletionDisabled()
        {
            SettingsStore.EnterTestMode(new WESettings
            {
                JavaScript = { BlockCommentCompletion = false }
            });

            string result = WriteBlockStarComment();
            result.Should().Be("/*\r\n");
        }

        private static string WriteBlockComment()
        {
            string fileName = CreateJavaScriptFile();
            var window = _dte.ItemOperations.OpenFile(fileName);

            SendKeys.SendWait("/");
            System.Threading.Thread.Sleep(500);
            SendKeys.SendWait("*");
            window.Document.Save();

            string result = File.ReadAllText(fileName);
            return result;
        }

        private static string WriteBlockStarComment()
        {
            var fileName = CreateJavaScriptFile();
            var window = _dte.ItemOperations.OpenFile(fileName);

            SendKeys.SendWait("/");
            System.Threading.Thread.Sleep(500);
            SendKeys.SendWait("*");
            System.Threading.Thread.Sleep(500);
            SendKeys.SendWait("{ENTER}");

            window.Document.Save();

            string result = File.ReadAllText(fileName);
            return result;
        }

        private static string CreateJavaScriptFile()
        {
            var fileName = Path.Combine(_testDir, Guid.NewGuid() + ".js");

            File.WriteAllText(fileName, string.Empty);
            return fileName;
        }
    }
}
