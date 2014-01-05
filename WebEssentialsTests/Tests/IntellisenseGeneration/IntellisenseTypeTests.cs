using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.Tests.IntellisenseGeneration
{
    [TestClass]
    public class IntellisenseType_JavaScript_Number
    {

        [TestMethod]
        public void JavaScriptType_for_int16_is_Number()
        {
            new IntellisenseType { CodeName = "Int16" }.JavaScriptName.Should().Be("Number");
        }
        [TestMethod]
        public void JavaScriptType_for_int32_is_Number()
        {
            new IntellisenseType { CodeName = "Int32" }.JavaScriptName.Should().Be("Number");
        }
        [TestMethod]
        public void JavaScriptType_for_int64_is_Number()
        {
            new IntellisenseType { CodeName = "Int64" }.JavaScriptName.Should().Be("Number");
        }
        [TestMethod]
        public void JavaScriptType_for_short_is_Number()
        {
            new IntellisenseType { CodeName = "Short" }.JavaScriptName.Should().Be("Number");
        }
        [TestMethod]
        public void JavaScriptType_for_int_is_Number()
        {
            new IntellisenseType { CodeName = "int" }.JavaScriptName.Should().Be("Number");
        }

        [TestMethod]
        public void JavaScriptType_for_long_is_Number()
        {
            new IntellisenseType { CodeName = "long" }.JavaScriptName.Should().Be("Number");
        }
        [TestMethod]
        public void JavaScriptType_for_float_is_Number()
        {
            new IntellisenseType { CodeName = "float" }.JavaScriptName.Should().Be("Number");
        }
        [TestMethod]
        public void JavaScriptType_for_double_is_Number()
        {
            new IntellisenseType { CodeName = "double" }.JavaScriptName.Should().Be("Number");
        }
        [TestMethod]
        public void JavaScriptType_for_decimal_is_Number()
        {
            new IntellisenseType { CodeName = "decimal" }.JavaScriptName.Should().Be("Number");
        }
        [TestMethod]
        public void JavaScriptType_for_biginteger_is_Number()
        {
            new IntellisenseType { CodeName = "biginteger" }.JavaScriptName.Should().Be("Number");
        }

    }
    [TestClass]
    public class IntellisenseType_TypeScript_Number
    {

        [TestMethod]
        public void TypeScriptType_for_int16_is_Number()
        {
            new IntellisenseType { CodeName = "Int16" }.TypeScriptName.Should().Be("number");
        }
        [TestMethod]
        public void TypeScriptType_for_int32_is_Number()
        {
            new IntellisenseType { CodeName = "Int32" }.TypeScriptName.Should().Be("number");
        }
        [TestMethod]
        public void TypeScriptType_for_int64_is_Number()
        {
            new IntellisenseType { CodeName = "Int64" }.TypeScriptName.Should().Be("number");
        }
        [TestMethod]
        public void TypeScriptType_for_short_is_Number()
        {
            new IntellisenseType { CodeName = "Short" }.TypeScriptName.Should().Be("number");
        }
        [TestMethod]
        public void TypeScriptType_for_int_is_Number()
        {
            new IntellisenseType { CodeName = "int" }.TypeScriptName.Should().Be("number");
        }

        [TestMethod]
        public void TypeScriptType_for_long_is_Number()
        {
            new IntellisenseType { CodeName = "long" }.TypeScriptName.Should().Be("number");
        }
        [TestMethod]
        public void TypeScriptType_for_float_is_Number()
        {
            new IntellisenseType { CodeName = "float" }.TypeScriptName.Should().Be("number");
        }
        [TestMethod]
        public void TypeScriptType_for_double_is_Number()
        {
            new IntellisenseType { CodeName = "double" }.TypeScriptName.Should().Be("number");
        }
        [TestMethod]
        public void TypeScriptType_for_decimal_is_Number()
        {
            new IntellisenseType { CodeName = "decimal" }.TypeScriptName.Should().Be("number");
        }
        [TestMethod]
        public void TypeScriptType_for_biginteger_is_Number()
        {
            new IntellisenseType { CodeName = "biginteger" }.TypeScriptName.Should().Be("number");
        }
    }

    [TestClass]
    public class IntellisenseType_JavaScript_Date
    {
        [TestMethod]
        public void JavaScriptType_for_datetime_is_Date()
        {
            new IntellisenseType { CodeName = "System.DateTime" }.JavaScriptName.Should().Be("Date");
            new IntellisenseType { CodeName = "DateTime" }.JavaScriptName.Should().Be("Date");
        }
        [TestMethod]
        public void JavaScriptType_for_DateTimeOffset_is_Date()
        {
            new IntellisenseType { CodeName = "System.DateTimeOffset" }.JavaScriptName.Should().Be("Date");
            new IntellisenseType { CodeName = "DateTimeOffset" }.JavaScriptName.Should().Be("Date");
        }
    }

    [TestClass]
    public class IntellisenseType_TypeScript_Date
    {
        [TestMethod]
        public void TypeScriptType_for_datetime_is_Date()
        {
            new IntellisenseType { CodeName = "System.DateTime" }.TypeScriptName.Should().Be("Date");
            new IntellisenseType { CodeName = "DateTime" }.TypeScriptName.Should().Be("Date");
        }
        [TestMethod]
        public void TypeScriptType_for_DateTimeOffset_is_Date()
        {
            new IntellisenseType { CodeName = "System.DateTimeOffset" }.TypeScriptName.Should().Be("Date");
            new IntellisenseType { CodeName = "DateTimeOffset" }.TypeScriptName.Should().Be("Date");
        }
    }

    [TestClass]
    public class IntellisenseType_TypeScript_Other
    {
        [TestMethod]
        public void TypeScriptType_for_string_is_string()
        {
            new IntellisenseType { CodeName = "String" }.TypeScriptName.Should().Be("string");
        }
        [TestMethod]
        public void TypeScriptType_bool_is_boolean()
        {
            new IntellisenseType { CodeName = "bool" }.TypeScriptName.Should().Be("boolean");
        }
        [TestMethod]
        public void TypeScriptType_boolean_is_boolean()
        {
            new IntellisenseType { CodeName = "Boolean" }.TypeScriptName.Should().Be("boolean");
        }
    }
    [TestClass]
    public class IntellisenseType_JavaScript_Other
    {
        [TestMethod]
        public void JavaScriptType_for_string_is_string()
        {
            new IntellisenseType { CodeName = "String" }.JavaScriptName.Should().Be("String");
        }
        [TestMethod]
        public void JavaScriptType_bool_is_boolean()
        {
            new IntellisenseType { CodeName = "bool" }.JavaScriptName.Should().Be("Boolean");
        }
        [TestMethod]
        public void JavaScriptType_boolean_is_boolean()
        {
            new IntellisenseType { CodeName = "Boolean" }.JavaScriptName.Should().Be("Boolean");
        }
    }
    [TestClass]
    public class IntellisenseType_JavaScriptLiteral
    {
        [TestMethod]
        public void JavaScriptLiteral_for_string()
        {
            new IntellisenseType { CodeName = "String" }.JavaScripLiteral.Should().Be("''");
        }
        [TestMethod]
        public void JavaScriptLiteral_for_bool()
        {
            new IntellisenseType { CodeName = "bool" }.JavaScripLiteral.Should().Be("false");
        }

        [TestMethod]
        public void JavaScriptLiteral_for_Int32()
        {
            new IntellisenseType { CodeName = "Int32" }.JavaScripLiteral.Should().Be("0");
        }

        [TestMethod]
        public void JavaScriptLiteral_for_Int32Array()
        {
            new IntellisenseType { CodeName = "Int32", IsArray = true }.JavaScripLiteral.Should().Be("[]");
        }
        [TestMethod]
        public void JavaScriptLiteral_for_Date()
        {
            new IntellisenseType { CodeName = "DateTime" }.JavaScripLiteral.Should().Be("new Date()");
        }
    }
}