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
    public class CompileOnSaveTests
    {
        static DTE DTE { get { return VsIdeTestHostContext.Dte; } }

        static string TestCaseDirectory { get; set; }

        [HostType("VS IDE")]
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            //DTE.ToString(); // Force initial launch to avoid funceval issues
            //System.Diagnostics.Debugger.Launch();
            SettingsStore.EnterTestMode();
            TestCaseDirectory = Path.Combine(Path.GetTempPath(), "Web Essentials Test Files", c.FullyQualifiedTestClassName + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            Directory.CreateDirectory(TestCaseDirectory);
        }

        [ClassCleanup]
        public static void DeleteTestCase()
        {
            Directory.Delete(TestCaseDirectory, true);
        }

        static async Task WaitFor(Func<bool> test, int maxSeconds)
        {
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalSeconds < maxSeconds)
            {
                if (test())
                    return;
                await Task.Delay(250);
            }
            Assert.Fail("Timed out waiting");
        }

        [HostType("VS IDE")]
        [TestMethod]
        public async Task CompileLessOnSaveWithoutProject()
        {
            SettingsStore.EnterTestMode();
            var fileName = Path.Combine(TestCaseDirectory, "Compile-" + Guid.NewGuid() + ".less");

            File.WriteAllText(fileName, @"a{b{color:red;}}");
            DTE.ItemOperations.OpenFile(fileName).Document.Save();
            await WaitFor(() => File.Exists(Path.ChangeExtension(fileName, ".css")), 10);
            File.Exists(Path.ChangeExtension(fileName, ".min.css")).Should().BeFalse("Should not minify by default");
        }

        [HostType("VS IDE")]
        [TestMethod]
        public async Task DontCompileOnOpen()
        {
            SettingsStore.EnterTestMode();
            var fileName = Path.Combine(TestCaseDirectory, "Don'tCompile-" + Guid.NewGuid() + ".coffee");

            File.WriteAllText(fileName, @"a{b{color:red;}}");
            DTE.ItemOperations.OpenFile(fileName);
            await Task.Delay(TimeSpan.FromSeconds(5));
            File.Exists(Path.ChangeExtension(fileName, ".js")).Should().BeFalse("Should not compile without saving");
        }

        [HostType("VS IDE")]
        [TestMethod]
        public async Task SkippableFiles()
        {
            SettingsStore.EnterTestMode();
            foreach (var baseName in new[] { "_underscore", "Sprite.png" })
            {
                var fileName = Path.Combine(TestCaseDirectory, baseName + ".less");

                File.WriteAllText(fileName, @"a{b{color:red;}}");
                DTE.ItemOperations.OpenFile(fileName).Document.Save();
                await Task.Delay(TimeSpan.FromSeconds(7.5));
                File.Exists(Path.ChangeExtension(fileName, ".css")).Should().BeFalse("Should not compile " + baseName + ".less");
            }
        }

        [HostType("VS IDE")]
        [TestMethod]
        public async Task MinifyOnSave()
        {
            SettingsStore.EnterTestMode();
            WESettings.Instance.Html.AutoMinify = true;
            WESettings.Instance.Html.GzipMinifiedFiles = true;
            WESettings.Instance.Markdown.CompileOnSave = true;

            var fileName = Path.Combine(TestCaseDirectory, "Minify-" + Guid.NewGuid() + ".md");
            var minFileName = Path.ChangeExtension(fileName, ".min.html");

            File.WriteAllText(fileName, "Hi\n#Header\n\n**Bold!**");
            File.Create(Path.Combine(minFileName)).Close();     // Only files that have a .min will be minified.

            DTE.ItemOperations.OpenFile(fileName).Document.Save();
            await WaitFor(() => new FileInfo(minFileName).Length > 0, 10);
        }
    }
}
