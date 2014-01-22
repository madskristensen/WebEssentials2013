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
        static readonly string BaseDirectory = Path.GetDirectoryName(typeof(ProjectHelpersTests).Assembly.Location);
        static readonly string FixtureDirectory = Path.Combine(BaseDirectory, "fixtures", "Visual Studio");
        static DTE DTE { get { return VsIdeTestHostContext.Dte; } }
        static IServiceProvider ServiceProvider { get { return VsIdeTestHostContext.ServiceProvider; } }

        [HostType("VS IDE")]
        [TestMethod]
        public void ProjectEnumerationTest()
        {
            SettingsStore.EnterTestMode();
            var solutionDir = Path.Combine(FixtureDirectory, "ProjectEnumeration");
            DTE.Solution.Open(Path.Combine(solutionDir, "ProjectEnumeration.sln"));
            var solutionService = ServiceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

            Project project = ProjectHelpers.GetAllProjects().First(p => p.Name == "CS-Normal");

            IVsHierarchy projHierarchy;
            ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(project.UniqueName, out projHierarchy));
            ErrorHandler.ThrowOnFailure(solutionService.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_UnloadProject, projHierarchy, 0));

            ProjectHelpers.GetAllProjects()
                          .Select(p =>
                                FileHelpers.RelativePath(
                                    solutionDir, ProjectHelpers.GetRootFolder(p)
                                ).TrimEnd('/')
                            )
                          .Should()
                          .BeEquivalentTo(
                                Directory.EnumerateDirectories(solutionDir)
                                     .Select(Path.GetFileName)
                                     .Except(new[] { "CS-Normal" })
                          );
        }

        // TODO: Test GetRootFolder() & other methods for each project type
    }
}
