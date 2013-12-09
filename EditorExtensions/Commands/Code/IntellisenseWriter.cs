using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell.Interop;

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
                if (io.Kind == "vsCMElementEnum")
                    continue;

                sb.AppendLine("server." + CamelCaseClassName(io.Name) + " = function()  {");

                foreach (var p in io.Properties)
                {
                    string value = GetJavaScriptTypes(p.Type);
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
            sb.AppendLine("declare module server {");
            sb.AppendLine();

            foreach (IntellisenseObject io in objects)
            {
                if (io.Kind == "vsCMElementEnum")
                {
                    sb.AppendLine("\tenum " + CamelCaseClassName(io.Name) + " {");
                }
                else
                {
                    sb.AppendLine("\tinterface " + CamelCaseClassName(io.Name) + " {");
                }

                foreach (var p in io.Properties)
                {
                    if (p.Type != null)
                    {
                        string value = GetTypeScriptType(p, io);
                        sb.AppendLine("\t\t" + CamelCasePropertyName(p.Name) + ": " + value + ";");
                    }
                    else
                    {
                        sb.AppendLine("\t\t" + CamelCasePropertyName(p.Name) + ",");
                    }
                }
                if (io.Kind == "vsCMElementEnum")
                {
                    sb.Remove(sb.Length - (Environment.NewLine.Length + 1), 1);
                }

                sb.AppendLine("\t}");
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

        private static string GetTypeScriptType(IntellisenseProperty ip, IntellisenseObject io)
        {
            var propertType = ip.Type.ToLowerInvariant();
            switch (propertType)
            {
                case "int":
                case "int32":
                case "int64":
                case "long":
                case "double":
                case "float":
                case "decimal":
                    return "number";

                case "system.datetime":
                    return "Date";

                case "string":
                    return "string";

                case "bool":
                case "boolean":
                    return "boolean";
            }

            if (propertType.Contains("system.collections") || propertType.EndsWith("[]"))
                return TypeScriptArrayName(ip.Type, io);
            if (propertType.Contains("Array"))
            {
                return "[]";
            }

            if (ip.Type.StartsWith(io.Namespace))
            {
                return ip.Type.Substring(ip.Type.LastIndexOf('.') + 1);
            }
            return "any";
        }

        private static string TypeScriptArrayName(string propertyName, IntellisenseObject io)
        {
            if (propertyName.EndsWith("[]"))
            {
                var name = propertyName.Substring(0, propertyName.Length - 2);
                var typeScriptType = GetTypeScriptType(new IntellisenseProperty()
                {
                    Name = name.Substring(name.LastIndexOf('.') + 1),
                    Type = name
                }, io);
                return typeScriptType + "[]";
            }
            //   01234<678>
            var startPos = propertyName.IndexOf('<');
            var endPos = propertyName.LastIndexOf('>');
            if (startPos <= 0 || endPos <= 0)
                return "[]";
            if (endPos < 0)
                return "[]";

            var typeName = propertyName.Substring(startPos + 1, endPos - startPos - 1);
            if (typeName.Contains(","))
                return "[]";

            var scriptType = GetTypeScriptType(new IntellisenseProperty()
            {
                Name = typeName.Substring(typeName.LastIndexOf('.') + 1),
                Type = typeName
            }, io);
            return scriptType + "[]";
        }

        private static string GetJavaScriptTypes(string type)
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
        public string Namespace { get; set; }
        public List<IntellisenseProperty> Properties { get; set; }
        public string Kind { get; set; }
    }

    public class IntellisenseProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Summary { get; set; }
    }
}