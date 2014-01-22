using System.Linq;
using EnvDTE;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebEssentialsTests.Tests.IntellisenseGeneration.TestHelper;

namespace WebEssentialsTests.Tests.IntellisenseGeneration
{
    [TestClass]
    public class Parsing_CollectionModel_Tests
    {
        private ProjectItem item;
        private IntellisenseObject theObject;


        [TestInitialize]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void Init()
        {
            item = VSHost.EnsureSolution(@"CodeGen\CodeGen.sln").FindProjectItem("CollectionModel.cs");
            theObject = IntellisenseParser.ProcessFile(item).First();
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_should_be_parsed()
        {
            theObject.Should().NotBeNull();
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_Name_is_CollectionModel()
        {
            theObject.Should().NameIs("CollectionModel");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_Property_AStringArray_is_correct()
        {
            theObject.Should().HasProperty("AStringArray")
                .And.IsArray()
                .And.JavaScriptNameIs("String")
                .And.TypeScriptNameIs("string");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_Property_AStringIEnumerable_is_correct()
        {
            theObject.Should().HasProperty("AStringIEnumerable")
                .And.IsArray()
                .And.JavaScriptNameIs("String")
                .And.TypeScriptNameIs("string");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_Property_AStringICollection_is_correct()
        {
            theObject.Should().HasProperty("AStringICollection")
                .And.IsArray()
                .And.JavaScriptNameIs("String")
                .And.TypeScriptNameIs("string");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_Property_AStringIList_is_correct()
        {
            theObject.Should().HasProperty("AStringIList")
                .And.IsArray()
                .And.JavaScriptNameIs("String")
                .And.TypeScriptNameIs("string");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_Property_AStringList_is_correct()
        {
            theObject.Should().HasProperty("AStringList")
                .And.IsArray()
                .And.JavaScriptNameIs("String")
                .And.TypeScriptNameIs("string");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_Property_AStringCollection_is_correct()
        {
            theObject.Should().HasProperty("AStringCollection")
                .And.IsArray()
                .And.JavaScriptNameIs("String")
                .And.TypeScriptNameIs("string");
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void CollectionModel_Property_ASimpleList_is_correct()
        {
            theObject.Should().HasProperty("ASimpleList")
                .And.IsArray()
                .And.JavaScriptNameIs("Object")
                .And.TypeScriptNameIs("server.Simple");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestProperty("VSTest", "VSTest")]
        public void CollectionModel_Property_ALongList_is_correct()
        {
            theObject.Should().HasProperty("ALongList")
                .And.IsArray()
                .And.JavaScriptNameIs("Number")
                .And.TypeScriptNameIs("number");
        }

    }
}