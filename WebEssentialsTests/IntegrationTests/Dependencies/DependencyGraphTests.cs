using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            SettingsStore.EnterTestMode();
        }

        static VsDependencyGraph graph;
        [TestInitialize]
        public void CreateGraph()
        {
            // Don't create the instance using MEF, to ensure that I get a fresh graph for each test.
            graph = new LessDependencyGraph(WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>().Value);
            graph.IsEnabled = true;
        }
        [TestCleanup]
        public void DisposeGraph()
        {
            graph.Dispose();
        }

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
                Path.Combine(VSHost.FixtureDirectory, "LessDependencies", "_admin.less")
            );
            adminDeps
                .Select(Path.GetFileName)
                .Should()
                .BeEquivalentTo(new[] { "_admin.less", "Manager.less" });

            var homeDeps = await graph.GetRecursiveDependentsAsync(
                Path.Combine(VSHost.FixtureDirectory, "LessDependencies", "Home.less")
            );
            homeDeps.Should().BeEmpty();
        }
    }
}
