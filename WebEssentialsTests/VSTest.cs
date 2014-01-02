using System;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace WebEssentialsTests
{
    public static class VSTest
    {
        static Lazy<Solution> Solution = new Lazy<Solution>(OpenSolution);

        private static Solution OpenSolution()
        {
            var path = typeof (VSTest).Assembly.Location;
            Path.GetDirectoryName(path);
            var solution = VsIdeTestHostContext.Dte.Solution;
            var fileName = Path.GetFullPath(Path.Combine(path, "..\\..\\..\\..\\TestProjects", "TestProjects.sln"));
            solution.Open(fileName);
            return solution;
        }

        public static Solution GetTestSolution()
        {
            return Solution.Value;
        }

    }

    [TestClass]
    public class VSTestTest
    {
        [TestMethod]
        [HostType("VS IDE")]
        public void VSTest_CanOpenSolution()
        {
            var solution = VSTest.GetTestSolution();
            solution.Should().NotBeNull();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void VSTest_CanFindProjectItem()
        {
            var solution = VSTest.GetTestSolution();
            var projectItem = solution.FindProjectItem("Simple.cs");
            projectItem.Should().NotBeNull();
        }
    }
}