using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    internal static class IntellisenseWriter
    {
        public static void Write(IEnumerable<IntellisenseObject> objects, string file)
        {
            StringBuilder sb = new StringBuilder();

            if (Path.GetExtension(file).Equals(".ts", StringComparison.OrdinalIgnoreCase))
                WriteTypeScript(objects, sb);
            else
                WriteJavaScript(objects, sb);

            WriteFileToDisk(file, sb);
        }

        private static string CamelCasePropertyName(string name)
        {
            if (WESettings.GetBoolean(WESettings.Keys.JavaScriptCamelCasePropertyNames))
            {
                name = CamelCase(name);
            }
            return name;
        }

        private static string CamelCaseClassName(string name)
        {
            if (WESettings.GetBoolean(WESettings.Keys.JavaScriptCamelCaseClassNames))
            {
                name = CamelCase(name);
            }
            return name;
        }

        private static string CamelCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            return name[0].ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture) + name.Substring(1);
        }

        private static string JsonPropertyName(IntellisenseProperty property, string fallbackName)
        {
            var attribute = property.Attributes.FirstOrDefault(a => a.Name == "JsonProperty");

            if (attribute != null)
            {
                if (attribute.Values.Count == 1 && attribute.Values.ContainsKey("")) // is the case you use the JsonProperty(string propertyName) attribute constructor
                {
                    return attribute.Values[""];
                }

                if (attribute.Values.ContainsKey("Name"))
                {
                    return attribute.Values["Name"];
                }
            }

            return fallbackName;
        }

        static readonly Regex whitespaceTrimmer = new Regex(@"^\s+|\s+$|\s*[\r\n]+\s*", RegexOptions.Compiled);

        private static void WriteJavaScript(IEnumerable<IntellisenseObject> objects, StringBuilder sb)
        {
            sb.AppendLine("var server = server || {};");

            foreach (IntellisenseObject io in objects)
            {
                if (io.IsEnum) continue;

                string comment = io.Summary ?? "The " + io.Name + " class as defined in " + io.FullName;
                comment = whitespaceTrimmer.Replace(comment, " ");
                sb.AppendLine("/// <summary>" + SecurityElement.Escape(comment) + "</summary>");
                sb.AppendLine("server." + CamelCaseClassName(io.Name) + " = function()  {");

                foreach (var p in io.Properties)
                {
                    string type = p.Type.JavaScriptName + (p.Type.IsArray ? "[]" : "");
                    var propertyName = JsonPropertyName(p, CamelCasePropertyName(p.Name));
                    comment = p.Summary ?? "The " + propertyName + " property as defined in " + io.FullName;
                    comment = whitespaceTrimmer.Replace(comment, " ");
                    sb.AppendLine("\t/// <field name=\"" + propertyName + "\" type=\"" + type + "\">" + SecurityElement.Escape(comment) + "</field>");
                    sb.AppendLine("\tthis." + propertyName + " = " + p.Type.JavaScripLiteral + ";");
                }

                sb.AppendLine("};");
                sb.AppendLine();
            }
        }

        private static void WriteTypeScript(IEnumerable<IntellisenseObject> objects, StringBuilder sb)
        {
            foreach (var ns in objects.GroupBy(o => o.Namespace))
            {
                sb.AppendFormat("declare module {0} {{\r\n", ns.Key);

                foreach (IntellisenseObject io in ns)
                {
                    if (!string.IsNullOrEmpty(io.Summary)) sb.AppendLine("\t/** " + whitespaceTrimmer.Replace(io.Summary, "") + " */");
                    if (io.IsEnum)
                    {
                        sb.AppendLine("\tenum " + CamelCaseClassName(io.Name) + " {");
                        foreach (var p in io.Properties)
                        {
                            WriteTypeScriptComment(p, sb);
                            sb.AppendLine("\t\t" + JsonPropertyName(p, CamelCasePropertyName(p.Name) + ","));
                        }
                        sb.AppendLine("\t}");
                    }
                    else
                    {
                        sb.Append("\tinterface ").Append(CamelCaseClassName(io.Name)).Append(" ");
                        WriteTSInterfaceDefinition(sb, "\t", io.Properties);
                        sb.AppendLine();
                    }
                }

                sb.AppendLine("}");
            }
        }

        private static void WriteTypeScriptComment(IntellisenseProperty p, StringBuilder sb)
        {
            if (string.IsNullOrEmpty(p.Summary)) return;
            sb.AppendLine("\t\t/** " + whitespaceTrimmer.Replace(p.Summary, "") + " */");
        }

        private static void WriteTSInterfaceDefinition(StringBuilder sb, string prefix, IEnumerable<IntellisenseProperty> props)
        {
            sb.AppendLine("{");

            foreach (var p in props)
            {
                WriteTypeScriptComment(p, sb);
                sb.AppendFormat("{0}\t{1}: ", prefix, JsonPropertyName(p, CamelCasePropertyName(p.Name)));

                if (p.Type.IsPrimitive) sb.Append(p.Type.TypeScriptName);
                else if (!string.IsNullOrEmpty(p.Type.ClientSideReferenceName)) sb.Append(p.Type.ClientSideReferenceName);
                else
                {
                    if (p.Type.Shape == null) sb.Append("any");
                    else WriteTSInterfaceDefinition(sb, prefix + "\t", p.Type.Shape);
                }
                if (p.Type.IsArray) sb.Append("[]");

                sb.AppendLine(";");
            }

            sb.Append(prefix).Append("}");
        }

        private static void WriteFileToDisk(string fileName, StringBuilder sb)
        {
            //string current = string.Empty;
            //if (File.Exists(fileName))
            //{
            //    current = File.ReadAllText(fileName);
            //}

            //if (current != sb.ToString())
            //{
            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
            //}
        }
    }

    public class IntellisenseObject
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsEnum { get; set; }
        public string Summary { get; set; }
        public IList<IntellisenseProperty> Properties { get; private set; }

        public IntellisenseObject()
        {
            Properties = new List<IntellisenseProperty>();
        }

        public IntellisenseObject(IList<IntellisenseProperty> properties)
        {
            Properties = properties;
        }
    }

    public class IntellisenseProperty
    {
        public string Name { get; set; }
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Unambiguous in this context.")]
        public IntellisenseType Type { get; set; }
        public string Summary { get; set; }
        public IList<IntellisenseAttribute> Attributes { get; set; }
    }

    public class IntellisenseAttribute
    {
        public string Name { get; set; }
        public Dictionary<string, string> Values { get; set; }
    }

    public class IntellisenseType
    {
        /// <summary>
        /// This is the name of this type as it appears in the source code
        /// </summary>
        public string CodeName { get; set; }
        /// <summary>
        /// Indicates whether this type is array. Is this property is true, then all other properties
        /// describe not the type itself, but rather the type of the array's elements.
        /// </summary>
        public bool IsArray { get; set; }
        /// <summary>
        /// If this type is itself part of a source code file that has a .d.ts definitions file attached,
        /// this property will contain the full (namespace-qualified) client-side name of that type.
        /// Otherwise, this property is null.
        /// </summary>
        public string ClientSideReferenceName { get; set; }
        /// <summary>
        /// This is TypeScript-formed shape of the type (i.e. inline type definition). It is used for the case where
        /// the type is not primitive, but does not have its own named client-side definition.
        /// </summary>
        public IEnumerable<IntellisenseProperty> Shape { get; set; }
        public bool IsPrimitive
        {
            get { return TypeScriptName != "any"; }
        }
        public string JavaScriptName
        {
            get { return GetTargetName(true); }
        }
        public string TypeScriptName
        {
            get { return GetTargetName(false); }
        }

        public string JavaScripLiteral
        {
            get
            {
                if (IsArray)
                    return "[]";
                switch (JavaScriptName)
                {
                    case "Number":
                        return "0";
                    case "String":
                        return "''";
                    case "Boolean":
                        return "false";
                    case "Array":
                        return "[]";
                    case "Object":
                        return "{ }";
                    default:
                        return "new " + JavaScriptName + "()";
                }
            }
        }


        string GetTargetName(bool js)
        {
            var t = CodeName.ToLowerInvariant().TrimEnd('?');
            switch (t)
            {
                case "int16":
                case "int32":
                case "int64":
                case "short":
                case "int":
                case "long":
                case "float":
                case "double":
                case "decimal":
                case "biginteger":
                    return js ? "Number" : "number";

                case "datetime":
                case "datetimeoffset":
                case "system.datetime":
                case "system.datetimeoffset":
                    return "Date";

                case "string":
                    return js ? "String" : "string";

                case "bool":
                case "boolean":
                    return js ? "Boolean" : "boolean";
            }

            return js ? "Object" : "any";
        }
    }
}
