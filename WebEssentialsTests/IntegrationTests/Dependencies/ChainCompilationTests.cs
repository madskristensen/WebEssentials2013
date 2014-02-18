using System;
using System.IO;
using System.Threading.Tasks;
using EnvDTE80;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.IntegrationTests.Dependencies
{
    [TestClass]
    public class ChainCompilationTests
    {
        static string TestCaseDirectory { get; set; }

        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            // Using FullyQualifiedTestClassName gives native PathTooLong errors when creating projects
            TestCaseDirectory = Path.Combine(Path.GetTempPath(), "Web Essentials Test Files", "ChainCompilationTests-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            Directory.CreateDirectory(TestCaseDirectory);

            SettingsStore.EnterTestMode();
            WESettings.Instance.Less.EnableChainCompilation = true;
            WESettings.Instance.Less.CompileOnSave = true;
        }

        [HostType("VS IDE")]
        [TestMethod]
        public async Task SaveDependentFiles()
        {
            var s2 = (Solution2)VSHost.DTE.Solution;
            s2.Create(TestCaseDirectory, "ChainCompilationTests");
            var template = s2.GetProjectTemplate("EmptyWebApplicationProject40.zip", "CSharp");
            s2.AddFromTemplate(template, Path.Combine(TestCaseDirectory, "WebAppProject"), "WebAppProject.csproj");

            // To be discovered on save of dependent file
            var mixinsPath = Path.Combine(TestCaseDirectory, "WebAppProject", "_mixins.less");
            string pagePath = Path.Combine(TestCaseDirectory, "WebAppProject", "page.less");
            string basePath = Path.Combine(TestCaseDirectory, "WebAppProject", "base.less");
            string otherDepPath = Path.Combine(TestCaseDirectory, "WebAppProject", "otherDep.less");

            File.WriteAllText(mixinsPath, "// Content...");
            AddProjectFile(basePath, "body { font: sans-serif }");
            AddProjectFile(pagePath, "@import 'base';");
            AddProjectFile(otherDepPath, "@import 'base';");

            File.WriteAllText(CssPath(basePath), "");
            File.WriteAllText(CssPath(pagePath), "");
            await Task.Delay(10);   // Give a tiny bit of time for the graph to initialize

            var window = VSHost.DTE.ItemOperations.OpenFile(basePath);
            await VSHost.TypeString("@import url(\"./_mixins.less\");\n");
            window.Document.Save();

            await WaitFor(() => new FileInfo(CssPath(basePath)).Length > 5, "base.less to compile", maxSeconds: 8);
            await WaitFor(() => new FileInfo(CssPath(pagePath)).Length > 5, "page.less to chain compile", maxSeconds: 2);
            File.Exists(CssPath(otherDepPath)).Should().BeFalse("Dependency without .css file should not be compiled");

            window = VSHost.DTE.ItemOperations.OpenFile(mixinsPath);
            await VSHost.TypeString(".MyClass { color: purple; }\n");
            window.Document.Save();

            await WaitFor(() => File.ReadAllText(CssPath(pagePath)).Contains(".MyClass"), "page.less to chain compile", maxSeconds: 2);
            File.Exists(CssPath(mixinsPath)).Should().BeFalse("File without .css file should not be compiled");
        }
        static async Task WaitFor(Func<bool> test, string message, int maxSeconds)
        {
            var start = DateTime.UtcNow;
            if (test()) return;
            await Task.Delay(10);
            if (test()) return;
            await Task.Delay(20);
            if (test()) return;

            while ((DateTime.UtcNow - start).TotalSeconds < maxSeconds)
            {
                if (test())
                    return;
                await Task.Delay(250);
            }
            Assert.Fail("Timed out waiting for " + message);
        }

        static string CssPath(string path) { return Path.ChangeExtension(path, ".css"); }
        static void AddProjectFile(string path, string contents)
        {
            File.WriteAllText(path, contents);
            ProjectHelpers.GetActiveProject().ProjectItems.AddFromFile(path);
        }
    }
}
