using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        static string CamelCasePropertyName(string name)
        {
            if (WESettings.GetBoolean(WESettings.Keys.JavaScriptCamelCasePropertyNames))
            {
                name = CamelCase(name);
            }
            return name;
        }
        static string CamelCaseClassName(string name)
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
                sb.AppendLine("server." + CamelCaseClassName(io.Name) + " = function()  {");

                foreach (var p in io.Properties)
                {
                    string value = GetValue(p.Type);
                    var propertyName = CamelCasePropertyName(p.Name);
                    string comment = p.Summary ?? "The " +propertyName+ " property as defined in " + io.FullName;
                    comment = Regex.Replace(comment, @"\s*[\r\n]+\s*", " ").Trim();
                    sb.AppendLine("\t/// <field name=\"" + propertyName + "\" type=\"" + value + "\">" + SecurityElement.Escape(comment) + "</field>");
                    sb.AppendLine("\tthis." + propertyName + " = new " + value + "();");
                }

                sb.AppendLine("};");
                sb.AppendLine();
            }
        }

        private static void WriteTypeScript(IEnumerable<IntellisenseObject> objects, StringBuilder sb)
        {
            sb.AppendLine("declare module server {");
            sb.AppendLine();

            foreach (IntellisenseObject io in objects)
            {
                sb.AppendLine("\tinterface " + CamelCaseClassName(io.Name) + "{");

                foreach (var p in io.Properties)
                {
                    string value = GetValue(p.Type);
                    sb.AppendLine("\t\t" + CamelCasePropertyName(p.Name) + ": " + value + ";");
                }

                sb.AppendLine("}");
            }

            sb.AppendLine("}");
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

        public static string GetValue(string type)
        {
            switch (type.ToLowerInvariant())
            {
                case "int":
                case "int32":
                case "int64":
                case "long":
                case "double":
                case "float":
                case "decimal":
                    return "Number";

                case "system.datetime":
                    return "Date";

                case "string":
                    return "String";

                case "bool":
                case "boolean":
                    return "Boolean";
            }

            if (type.Contains("System.Collections") || type.Contains("[]") || type.Contains("Array"))
                return "Array";

            return "Object";
        }
    }

    public class IntellisenseObject
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public List<IntellisenseProperty> Properties { get; set; }
    }

    public class IntellisenseProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Summary { get; set; }
    }
}
