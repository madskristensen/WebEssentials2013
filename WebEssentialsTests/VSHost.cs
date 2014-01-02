using System;
using System.IO;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace WebEssentialsTests
{
    public static class VSHost
    {
        static readonly Lazy<Solution> Solution = new Lazy<Solution>(OpenSolution);

        private static Solution OpenSolution()
        {
            var path = typeof(VSHost).Assembly.Location;
            Path.GetDirectoryName(path);
            var solution = VsIdeTestHostContext.Dte.Solution;
            var fileName = Path.GetFullPath(Path.Combine(path, @"..\..\..\..\TestProjects", "TestProjects.sln"));
            solution.Open(fileName);
            return solution;
        }

        public static Solution TestSolution { get { return Solution.Value; } }
    }

    [TestClass]
    public class VSHostTest
    {
        [TestMethod]
        [HostType("VS IDE")]
        public void VSTest_CanOpenSolution()
        {
            VSHost.TestSolution.Should().NotBeNull();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void VSTest_CanFindProjectItem()
        {
            var projectItem = VSHost.TestSolution.FindProjectItem("Simple.cs");
            projectItem.Should().NotBeNull();
        }
    }
}