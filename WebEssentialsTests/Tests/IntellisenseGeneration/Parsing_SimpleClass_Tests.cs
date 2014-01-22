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
        private ProjectItem _item;
        private IntellisenseObject _theObject;


        [TestInitialize]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Init()
        {
            _item = VSHost.EnsureSolution(@"CodeGen\CodeGen.sln").FindProjectItem("Simple.cs");
            _theObject = IntellisenseParser.ProcessFile(_item).First();
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Simple_should_be_parsed()
        {
            _theObject.Should().NotBeNull();
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Simple_Name_is_Simple()
        {
            _theObject.Should().NameIs("Simple");
        }

        [TestMethod]
        [TestProperty("VSTest", "VSTest")]
        [HostType("VS IDE")]
        public void Simple_AString_is_correct()
        {
            _theObject.Should().HasProperty("AString")
                .And.IsNotAnArray()
                .And.JavaScriptNameIs("String")
                .And.TypeScriptNameIs("string");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Simple_AnInt_is_correct()
        {
            _theObject.Should().HasProperty("AnInt")
                .And.IsNotAnArray()
                .And.JavaScriptNameIs("Number")
                .And.TypeScriptNameIs("number");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Simple_ABool_is_correct()
        {
            _theObject.Should().HasProperty("ABool")
                .And.IsNotAnArray()
                .And.JavaScriptNameIs("Boolean")
                .And.TypeScriptNameIs("boolean");
        }
        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Simple_ASimple_property_is_correct()
        {
            _theObject.Should().HasProperty("ASimple")
                .And.IsNotAnArray()
                .And.JavaScriptNameIs("Object")
                .And.TypeScriptNameIs("server.Simple");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Simple_ADateTime_property_is_correct()
        {
            _theObject.Should().HasProperty("ADateTime")
                .And.JavaScriptNameIs("Date")
                .And.TypeScriptNameIs("Date");
        }
    }
}
