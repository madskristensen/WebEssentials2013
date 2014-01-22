using System.Linq;
using EnvDTE;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebEssentialsTests.Tests.IntellisenseGeneration.TestHelper;

namespace WebEssentialsTests.Tests.IntellisenseGeneration
{
    [TestClass]
    public class Parsing_SimpleNullable_Tests
    {
        private ProjectItem _item;
        private IntellisenseObject _theObject;


        [TestInitialize]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Init()
        {
            _item = VSHost.EnsureSolution(@"CodeGen\CodeGen.sln").FindProjectItem("SimpleNullable.cs");
            _theObject = IntellisenseParser.ProcessFile(_item).First();
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void SimpleNullable_should_be_parsed()
        {
            _theObject.Should().NotBeNull();
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void SimpleNullable_Name_is_Simple()
        {
            _theObject.Should().NameIs("SimpleNullable");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void SimpleNullable_AnInt_is_correct()
        {
            _theObject.Should().HasProperty("AnInt")
                .And.IsNotAnArray()
                .And.JavaScriptNameIs("Number")
                .And.TypeScriptNameIs("number");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void SimpleNullable_ABool_is_correct()
        {
            _theObject.Should().HasProperty("ABool")
                .And.IsNotAnArray()
                .And.JavaScriptNameIs("Boolean")
                .And.TypeScriptNameIs("boolean");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void SimpleNullable_ADateTime_property_is_correct()
        {
            _theObject.Should().HasProperty("ADateTime")
                .And.IsNotAnArray()
                .And.JavaScriptNameIs("Date")
                .And.TypeScriptNameIs("Date");
        }
    }
}