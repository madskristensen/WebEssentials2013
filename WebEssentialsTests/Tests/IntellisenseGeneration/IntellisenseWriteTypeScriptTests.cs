using System.Text;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.Tests.IntellisenseGeneration
{
    [TestClass]
    public class IntellisenseTypescript_with_primitives
    {
        private readonly IntellisenseType _stringType = new IntellisenseType { CodeName = "String" };
        private readonly IntellisenseType _int32Type = new IntellisenseType { CodeName = "Int32" };
        private readonly IntellisenseType _int32ArrayType = new IntellisenseType { CodeName = "Int32", IsArray = true };
        private readonly IntellisenseType _simpleType = new IntellisenseType { CodeName = "Foo.Simple", ClientSideReferenceName = "server.Simple" };
        private readonly IntellisenseType _stringDictionary = new IntellisenseType { CodeName = "Dictionary<string, string>", IsDictionary = true };
        private readonly IntellisenseType _numberDictionary = new IntellisenseType { CodeName = "Dictionary<int, string>", IsDictionary = true };
        private readonly IntellisenseType _objectDictionary = new IntellisenseType { CodeName = "Dictionary<string, object>", IsDictionary = true };
        private readonly IntellisenseType _GuidObjectDictionary = new IntellisenseType { CodeName = "Dictionary<Guid, object>", IsDictionary = true };

        [TestMethod]
        public void TypeScript_with_on_string_property()
        {
            var result = new StringBuilder();

            var io = new IntellisenseObject(new[]
            {
                new IntellisenseProperty(_stringType, "AString")
            })
            {
                FullName = "Foo.Primitives",
                Name = "Primitives",
                Namespace = "server"
            };
            IntellisenseWriter.WriteTypeScript(new[] { io }, result);

            result.ShouldBeCode(@"
declare module server {
       interface Primitives {
        aString: string;
    }
}");
        }

        [TestMethod]
        public void TypeScript_with_a_string_an_int_and_and_int_arrayproperty()
        {
            var result = new StringBuilder();

            var io = new IntellisenseObject(new[]
            {
                new IntellisenseProperty(_stringType, "AString"),
                new IntellisenseProperty(_int32Type, "AnInt"),
                new IntellisenseProperty(_int32ArrayType, "AnIntArray") { Summary = "ASummary"},
                new IntellisenseProperty(_simpleType, "TheSimple")
            })
            {
                FullName = "Foo.Primitives",
                Name = "Primitives",
                Namespace = "server"
            };
            IntellisenseWriter.WriteTypeScript(new[] { io }, result);

            result.ShouldBeCode(@"
declare module server {
       interface Primitives {
        aString: string;
        anInt: number;
/** ASummary */
        anIntArray: number[];
        theSimple: server.Simple;
}
}");
        }

        [TestMethod]
        public void TypeScript_with_a_string_and_simple_dictionary_property()
        {
            var result = new StringBuilder();

            var io = new IntellisenseObject(new[]
            {
                new IntellisenseProperty(_stringType, "AString"),
                new IntellisenseProperty(_stringDictionary, "ADictionary")
            })
            {
                FullName = "Foo",
                Name = "Bar",
                Namespace = "server"
            };
            IntellisenseWriter.WriteTypeScript(new[] { io }, result);

            result.ShouldBeCode(@"
                declare module server {
                       interface Bar {
                        aString: string;
                        aDictionary: { [index: string]: string };
                    }
                }"
            );
        }

        [TestMethod]
        public void TypeScript_with_a_string_and_number_dictionary_property()
        {
            var result = new StringBuilder();

            var io = new IntellisenseObject(new[]
            {
                new IntellisenseProperty(_stringType, "AString"),
                new IntellisenseProperty(_numberDictionary, "ADictionary")
            })
            {
                FullName = "Foo",
                Name = "Bar",
                Namespace = "server"
            };
            IntellisenseWriter.WriteTypeScript(new[] { io }, result);

            result.ShouldBeCode(@"
                declare module server {
                       interface Bar {
                        aString: string;
                        aDictionary: { [index: number]: string };
                    }
                }"
            );
        }

        [TestMethod]
        public void TypeScript_with_a_string_and_object_dictionary_property()
        {
            var result = new StringBuilder();

            var io = new IntellisenseObject(new[]
            {
                new IntellisenseProperty(_stringType, "AString"),
                new IntellisenseProperty(_GuidObjectDictionary, "ADictionary")
            })
            {
                FullName = "Foo",
                Name = "Bar",
                Namespace = "server"
            };
            IntellisenseWriter.WriteTypeScript(new[] { io }, result);

            result.ShouldBeCode(@"
                declare module server {
                       interface Bar {
                        aString: string;
                        aDictionary: { [index: string]: any };
                    }
                }"
            );
        }

    }
}