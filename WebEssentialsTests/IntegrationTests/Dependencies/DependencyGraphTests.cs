using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE80;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace WebEssentialsTests.IntegrationTests.Dependencies
{
    [TestClass]
    public class DependencyGraphTests
    {
        static string TestCaseDirectory { get; set; }

        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            // Using FullyQualifiedTestClassName gives native PathTooLong errors when creating projects
            TestCaseDirectory = Path.Combine(Path.GetTempPath(), "Web Essentials Test Files", "DependencyGraphTests-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            Directory.CreateDirectory(TestCaseDirectory);

            SettingsStore.EnterTestMode();
        }

        static VsDependencyGraph graph;
        static ComposablePart graphPart;
        [TestInitialize]
        public void CreateGraph()
        {
            // Make sure VS has loaded before using MEF (prevents native access violations!)
            VSHost.EnsureSolution(@"LessDependencies\LessDependencies.sln");

            // Don't create the instance using MEF, to ensure that I get a fresh graph for each test.
            graph = new LessDependencyGraph(WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>().Value);
            graph.IsEnabled = true;

            // Add the instance to the MEF catalog so that its IFileSaveListener is picked up
            graphPart = AttributedModelServices.CreatePart(graph);
            var cc = (CompositionContainer)WebEditor.CompositionService;
            cc.Compose(new CompositionBatch(new[] { graphPart }, null));
        }
        [TestCleanup]
        public void DisposeGraph()
        {
            graph.Dispose();
            var cc = (CompositionContainer)WebEditor.CompositionService;
            cc.Compose(new CompositionBatch(null, new[] { graphPart }));
        }

        [HostType("VS IDE")]
        [TestMethod]
        public async Task ExistingDependenciesFromSolution()
        {
            VSHost.EnsureSolution(@"LessDependencies\LessDependencies.sln");
            await graph.RescanComplete;
            var sharedDeps = await graph.GetRecursiveDependentsAsync(
                Path.Combine(VSHost.FixtureDirectory, "LessDependencies", "_shared.less")
            );
            sharedDeps
                .Select(Path.GetFileName)
                .Should()
                .BeEquivalentTo(new[] { "_admin.less", "Manager.less", "Home.less" });

            var adminDeps = await graph.GetRecursiveDependentsAsync(
                Path.Combine(VSHost.FixtureDirectory, "LessDependencies", "Areas", "_admin.less")
            );
            adminDeps
                .Select(Path.GetFileName)
                .Should()
                .BeEquivalentTo(new[] { "Manager.less" });

            var homeDeps = await graph.GetRecursiveDependentsAsync(
                Path.Combine(VSHost.FixtureDirectory, "LessDependencies", "Home.less")
            );
            homeDeps.Should().BeEmpty();
        }
        [HostType("VS IDE")]
        [TestMethod]
        public async Task DependenciesFromNewFiles()
        {
            var s2 = (Solution2)VSHost.DTE.Solution;
            s2.Create(TestCaseDirectory, "DependencyCreationTests");
            var template = s2.GetProjectTemplate("EmptyWebApplicationProject40.zip", "CSharp");
            s2.AddFromTemplate(template, Path.Combine(TestCaseDirectory, "WebAppProject"), "WebAppProject.csproj");

            // To be discovered on save of dependent file
            File.WriteAllText(Path.Combine(TestCaseDirectory, "WebAppProject", "_mixins.less"), "// Content...");

            AddProjectFile(Path.Combine(TestCaseDirectory, "WebAppProject", "base.less"), "body { font: sans-serif }");

            AddProjectFile(Path.Combine(TestCaseDirectory, "WebAppProject", "page.less"), "@import 'base';");

            await graph.RescanComplete;

            var deps = await graph.GetRecursiveDependentsAsync(
                Path.Combine(TestCaseDirectory, "WebAppProject", "base.less")
            );
            deps
               .Select(Path.GetFileName)
               .Should()
               .BeEquivalentTo(new[] { "page.less" });

            var window = VSHost.DTE.ItemOperations.OpenFile(Path.Combine(TestCaseDirectory, "WebAppProject", "base.less"));
            await VSHost.TypeString("@import url(\"./_mixins\");\n");
            window.Document.Save();

            await graph.RescanComplete;

            deps = await graph.GetRecursiveDependentsAsync(
                Path.Combine(TestCaseDirectory, "WebAppProject", "_mixins.less")
            );
            deps
               .Select(Path.GetFileName)
               .Should()
               .BeEquivalentTo(new[] { "base.less", "page.less" });
        }

        static void AddProjectFile(string path, string contents)
        {
            File.WriteAllText(path, contents);
            ProjectHelpers.GetActiveProject().ProjectItems.AddFromFile(path);
        }
    }
}
