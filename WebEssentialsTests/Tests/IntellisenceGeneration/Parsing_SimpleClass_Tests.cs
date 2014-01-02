using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using EnvDTE;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.Tests.IntellisenceGeneration
{
    public static class IntellisenseTestExtensions
    {
        public static bool HasProperty(this IntellisenseObject o, string propertyName)
        {
            return o.Properties.Any(p => p.Name == propertyName);
        }

        public static IntellisenseProperty GetProperty(this IntellisenseObject o, string propertyName)
        {
            return o.Properties.Single(p => p.Name == propertyName);
        }
    }

    [TestClass]
    public class Parsing_SimpleClass_Tests
    {
        private ProjectItem item;
        private IntellisenseObject theObject;


        [TestInitialize]
        [HostType("VS IDE")]
        public void Init()
        {
            item = VSTest.GetTestSolution().FindProjectItem("Simple.cs");
            theObject = IntellisenseParser.ProcessFile(item).First();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void Simple_should_be_parsed()
        {
            theObject.Should().NotBeNull();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void Simple_Name_is_Simple()
        {
            theObject.Name.Should().Equals("Simple");
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void Simple_has_properties()
        {
            theObject.HasProperty("AString").Should().BeTrue();
            theObject.HasProperty("ABool").Should().BeTrue();
            theObject.HasProperty("AnInt").Should().BeTrue();
        }

        [TestMethod()]
        [HostType("VS IDE")]
        [Ignore]
        public void Run_this_if_you_want_to_configure_your_hive_like_enabling_or_disabling_some_addins_in_special_webEssentials()
        {
            System.Threading.Thread.Sleep(1000*60*20);
        }
    }
}