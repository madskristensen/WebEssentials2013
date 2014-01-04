using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebEssentialsTests.Tests.IntellisenseGeneration.TestHelper;

namespace WebEssentialsTests.Tests.IntellisenseGeneration
{
    [TestClass]
    public class IntellisenseTypescript_with_primitives
    {
        private readonly IntellisenseType _stringType = new IntellisenseType() {CodeName = "string"};

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
            IntellisenseWriter.WriteTypeScript(new[] {io}, result);

            result.ShouldBeCode(@"
declare module server {
       interface Primitives {
        AString: string;
    }
}");
        }

    }
}