using System;
using System.IO;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.IntegrationTests
{
    [TestClass]
    public class ProjectHelpersTests
    {
        [HostType("VS IDE")]
        [ClassInitialize]
        public static void Initialize(TestContext c)
        {
            SettingsStore.EnterTestMode();
            VSHost.EnsureSolution(@"ProjectEnumeration\ProjectEnumeration.sln");
        }

        [HostType("VS IDE")]
        [TestMethod]
        public void ProjectEnumerationTest()
        {
            var solutionService = VSHost.GetService<IVsSolution>(typeof(SVsSolution));

            Project project = ProjectHelpers.GetAllProjects().First(p => p.Name == "CS-Normal");

            IVsHierarchy projHierarchy;
            ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(project.UniqueName, out projHierarchy));
            ErrorHandler.ThrowOnFailure(solutionService.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_UnloadProject, projHierarchy, 0));

            // We target VS2013, which can only open WinStore
            // apps when running on Win8.1. Therefore, I skip
            // those projects when testing on older platforms
            var isWin81 = Type.GetType("Windows.UI.Xaml.Controls.Flyout, Windows.UI.Xaml, ContentType=WindowsRuntime", false) != null;

            ProjectHelpers.GetAllProjects()
                          .Select(ProjectHelpers.GetRootFolder)
                          .Select(f => f.TrimEnd('\\'))
                          .Should()
                          .BeEquivalentTo(
                                Directory.EnumerateDirectories(Path.Combine(VSHost.FixtureDirectory, "ProjectEnumeration"))
                                     .Where(f => Path.GetFileName(f) != "CS-Normal" && Path.GetFileName(f) != "Debug")  // Skip temp folder & unloaded project
                                     .Where(f => isWin81 || !f.Contains("WinStore"))
                          );
        }

        // TODO: Test other methods for each project type
        [TestMethod]
        public void IsWebProjectTest()
        {
            foreach (var project in ProjectHelpers.GetAllProjects())
            {
                project.IsWebProject().Should().Be(project.Name.StartsWith("Web") || project.Name.StartsWith("JS"),
                                                   project.Name + " should be detected");
            }
        }
    }
}
