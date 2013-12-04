using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    internal class IntellisenseWriter
    {
        public void Write(List<IntellisenseObject> objects, string file)
        {
            StringBuilder sb = new StringBuilder();

            if (Path.GetExtension(file).Equals(".ts", StringComparison.OrdinalIgnoreCase))
                WriteTypeScript(objects, sb);
            else
                WriteJavaScript(objects, sb);

            WriteFileToDisk(file, sb);
        }

        private static void WriteJavaScript(List<IntellisenseObject> objects, StringBuilder sb)
        {
            sb.AppendLine("var server = server || {};");

            // all /// <field/> entries must be listed at the very top, otherwise Intellisense won't work for subsequent entries
            StringBuilder typeLiteralBuilder = new StringBuilder();

            foreach (IntellisenseObject io in objects)
            {
                sb.AppendLine("server." + io.Name + " = function()  {");

                foreach (var p in io.Properties)
                {
                    string typeName = GetTypeName(p.Type, Language.JavaScript);
                    string typeLiteral = GetTypeLiteral(p.Type);
                    string comment = p.Summary ?? "The " + p.Name + " property as defined in " + io.FullName;
                    comment = Regex.Replace(comment, @"\s*[\r\n]+\s*", " ").Trim();
                    sb.AppendLine("\t/// <field name=\"" + p.Name + "\" type=\"" + typeName + "\">" + SecurityElement.Escape(comment) + "</field>");
                    typeLiteralBuilder.AppendLine("\tthis." + p.Name + " = " + typeLiteral + ";");
                }

                sb.Append(typeLiteralBuilder.ToString());

                sb.AppendLine("};");
                sb.AppendLine();
            }
        }

        private static void WriteTypeScript(List<IntellisenseObject> objects, StringBuilder sb)
        {
            sb.AppendLine("declare module server {");
            sb.AppendLine();

            foreach (IntellisenseObject io in objects)
            {
                sb.AppendLine("\tinterface " + io.Name + "{");

                foreach (var p in io.Properties)
                {
                    string typeName = GetTypeName(p.Type, Language.TypeScript);
                    sb.AppendLine("\t\t" + p.Name + ": " + typeName + ";");
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

        public static string GetTypeName(string type, Language language)
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
                    return language == Language.JavaScript ? "Number" : "number";

                case "system.datetime":
                    return "Date";

                case "string":
                    return language == Language.JavaScript ? "String" : "string";

                case "bool":
                case "boolean":
                    return language == Language.JavaScript ? "Boolean" : "boolean";
            }

            if (type.Contains("System.Collections") || type.Contains("[]") || type.Contains("Array"))
                return "Array";

            return "Object";
        }

        public static string GetTypeLiteral(string type)
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
                    return "0";

                case "system.datetime":
                    return "new Date()";

                case "string":
                    return "''";

                case "bool":
                case "boolean":
                    return "false";
            }

            if (type.Contains("System.Collections") || type.Contains("[]") || type.Contains("Array"))
                return "[]";

            return "{}";
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
    
    public enum Language
    {
		JavaScript,
		TypeScript
    }
}
