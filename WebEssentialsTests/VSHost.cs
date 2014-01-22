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
        static readonly string BaseDirectory = Path.GetDirectoryName(typeof(VSHost).Assembly.Location);
        public static readonly string FixtureDirectory = Path.Combine(BaseDirectory, "fixtures", "Visual Studio");

        public static DTE DTE { get { return VsIdeTestHostContext.Dte; } }
        public static IServiceProvider ServiceProvider { get { return VsIdeTestHostContext.ServiceProvider; } }

        public static T GetService<T>(Type idType) { return (T)ServiceProvider.GetService(idType); }

        ///<summary>Ensures that the specified solution is open.</summary>
        ///<param name="relativePath">The path to the solution file, relative to fixtures\Visual Studio.</param>
        public static Solution EnsureSolution(string relativePath)
        {
            var fileName = Path.GetFullPath(Path.Combine(FixtureDirectory, relativePath));
            if (!File.Exists(fileName))
                throw new FileNotFoundException("Solution file does not exist", fileName);
            var solution = VsIdeTestHostContext.Dte.Solution;
            if (solution.FullName != fileName)
                solution.Open(fileName);
            return solution;
        }
    }
}