using System;
using System.IO;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

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

            ProjectHelpers.GetAllProjects()
                          .Select(ProjectHelpers.GetRootFolder)
                          .Select(f => f.TrimEnd('\\'))
                          .Should()
                          .BeEquivalentTo(
                                Directory.EnumerateDirectories(Path.Combine(VSHost.FixtureDirectory, "ProjectEnumeration"))
                                     .Where(f => Path.GetFileName(f) != "CS-Normal")
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
