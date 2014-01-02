using System.Linq;
using EnvDTE;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebEssentialsTests.Tests.IntellisenseGeneration.TestHelper;

namespace WebEssentialsTests.Tests.IntellisenseGeneration
{
    [TestClass]
    public class Parsing_SimpleClass_Tests
    {
        private ProjectItem item;
        private IntellisenseObject theObject;


        [TestInitialize]
        [HostType("VS IDE")]
        public void Init()
        {
            item = VSHost.TestSolution.FindProjectItem("Simple.cs");
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
            theObject.Should().NameIs("Simple");
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void Simple_has_properties()
        {
            theObject.Should().HasProperty("AString");
            theObject.Should().HasProperty("ABool");
            theObject.Should().HasProperty("AnInt");
        }

        [TestMethod()]
        [HostType("VS IDE")]
        [Ignore]
        public void
            Run_this_if_you_want_to_configure_your_hive_like_enabling_or_disabling_some_addins_in_special_webEssentials()
        {
            System.Threading.Thread.Sleep(1000 * 60 * 20);
        }
    }
}