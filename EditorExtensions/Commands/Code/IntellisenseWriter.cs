using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    internal class IntellisenseWriter
    {
        public void Write(IEnumerable<IntellisenseObject> objects, string file)
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
            return name[0].ToString(CultureInfo.CurrentCulture).ToLower() + name.Substring(1);
        }

        private static void WriteJavaScript(IEnumerable<IntellisenseObject> objects, StringBuilder sb)
        {
            sb.AppendLine("var server = server || {};");

            foreach (IntellisenseObject io in objects)
            {
                if (io.IsEnum) continue;

                sb.AppendLine("server." + CamelCaseClassName(io.Name) + " = function()  {");

                foreach (var p in io.Properties)
                {
                    string value = p.Type.GetJavaScriptName() + (p.Type.IsArray ? "[]" : "");
                    var propertyName = CamelCasePropertyName(p.Name);
                    string comment = p.Summary ?? "The " + propertyName + " property as defined in " + io.FullName;
                    comment = Regex.Replace(comment, @"\s*[\r\n]+\s*", " ").Trim();
                    sb.AppendLine("\t/// <field name=\"" + propertyName + "\" type=\"" + value + "\">" +
                                  SecurityElement.Escape(comment) + "</field>");
                    sb.AppendLine("\tthis." + propertyName + " = new " + value + "();");
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
                    if (io.IsEnum)
                    {
                        sb.AppendLine("\tenum " + CamelCaseClassName(io.Name) + " {");
                        foreach (var p in io.Properties) sb.AppendLine("\t\t" + CamelCasePropertyName(p.Name) + ",");
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

        private static void WriteTSInterfaceDefinition(StringBuilder sb, string prefix, IEnumerable<IntellisenseProperty> props)
        {
            sb.AppendLine("{");

            foreach (var p in props)
            {
                sb.AppendFormat("{0}\t{1}: ", prefix, CamelCasePropertyName(p.Name));

                if (p.Type.IsPrimitive()) sb.Append(p.Type.GetTypeScriptName());
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
            File.WriteAllText(fileName, sb.ToString());
            //}
        }
    }

    public class IntellisenseObject
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsEnum { get; set; }
        public List<IntellisenseProperty> Properties { get; set; }
    }

    public class IntellisenseProperty
    {
        public string Name { get; set; }
        public IntellisenseType Type { get; set; }
        public string Summary { get; set; }
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

        public bool IsPrimitive() { return GetTypeScriptName() != "any"; }
        public string GetJavaScriptName() { return GetTargetName(true); }
        public string GetTypeScriptName() { return GetTargetName(false); }

        string GetTargetName(bool js)
        {
            var t = CodeName.ToLower();
            switch (t)
            {
                case "int":
                case "int32":
                case "int64":
                case "long":
                case "double":
                case "float":
                case "decimal":
                    return js ? "Number" : "number";

                case "system.datetime":
                    return "Date";

                case "string":
                    return js ? "String" : "string";

                case "bool":
                case "boolean":
                    return js ? "Boolean" : "boolean";
            }

            if (t.Contains("system.collections") || t.Contains("[]") || t.Contains("array"))
                return "Array";

            return js ? "Object" : "any";
        }
    }
}