using System.Linq;
using EnvDTE;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebEssentialsTests.Tests.IntellisenseGeneration;
using WebEssentialsTests.Tests.IntellisenseGeneration.TestHelper;

namespace WebEssentialsTests.Tests.IntellisenceGeneration
{
    [TestClass]
    public class Parsing_CollectionModel_Tests
    {
        private ProjectItem item;
        private IntellisenseObject theObject;


        [TestInitialize]
        [HostType("VS IDE")]
        public void Init()
        {
            item = VSTest.GetTestSolution().FindProjectItem("CollectionModel.cs");
            theObject = IntellisenseParser.ProcessFile(item).First();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void CollectionModel_should_be_parsed()
        {
            theObject.Should().NotBeNull();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void CollectionModel_Name_is_CollectionModel()
        {
            theObject.Should().NameIs("CollectionModel");
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void CollectionModel_Property_AStringArray_is_correct()
        {
            theObject.Should().HasProperty("AStringArray")
                .And.IsArray()
                .And.JavaScriptNameIs("String")
                .And.TypeScriptNameIs("string");

        }
    }
}