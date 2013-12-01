using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    internal static class IntellisenseWriter
    {
        public static void Write(List<IntellisenseObject> objects, string file)
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

            foreach (IntellisenseObject io in objects)
            {
                sb.AppendLine("server." + io.Name + " = function()  {");

                foreach (var p in io.Properties)
                {
                    string value = GetValue(p.Type);
                    string comment = p.Summary ?? "The " + p.Name + " property as defined in " + io.FullName;
                    comment = Regex.Replace(comment, @"\s*[\r\n]+\s*", " ").Trim();
                    sb.AppendLine("\t/// <field name=\"" + p.Name + "\" type=\"" + value + "\">" + SecurityElement.Escape(comment) + "</field>");
                    sb.AppendLine("\tthis." + p.Name + " = new " + value + "();");
                }

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
                    string value = GetValue(p.Type);
                    sb.AppendLine("\t\t" + p.Name + ": " + value + ";");
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

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Intellisense")]
    public class IntellisenseObject
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public List<IntellisenseProperty> Properties { get; set; }
    }

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Intellisense")]
    public class IntellisenseProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Summary { get; set; }
    }
}
