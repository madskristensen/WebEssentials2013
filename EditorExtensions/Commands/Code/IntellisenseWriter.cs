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
    internal static class IntellisenseWriter
    {
        public static void Write(IEnumerable<IntellisenseObject> objects, string file)
        {
            StringBuilder sb = new StringBuilder();

            if (Path.GetExtension(file).Equals(".ts", StringComparison.OrdinalIgnoreCase))
                WriteTypeScript(objects, sb, file);
            else
                WriteJavaScript(objects, sb);

            File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
        }

        private static string CamelCasePropertyName(string name)
        {
            if (WESettings.Instance.CodeGen.CamelCasePropertyNames)
            {
                name = CamelCase(name);
            }
            return name;
        }

        private static string CamelCaseClassName(string name)
        {
            if (WESettings.Instance.CodeGen.CamelCaseTypeNames)
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

        private static string CleanEnumInitValue(string value)
        {
            value = value.TrimEnd('u', 'U', 'l', 'L'); //uint ulong long
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return value;
            var trimedValue = value.TrimStart('0'); // prevent numbers to be parsed as octal in js.
            if (trimedValue.Length > 0) return trimedValue;
            return "0";
        }

        private static readonly Regex whitespaceTrimmer = new Regex(@"^\s+|\s+$|\s*[\r\n]+\s*", RegexOptions.Compiled);

        internal static void WriteJavaScript(IEnumerable<IntellisenseObject> objects, StringBuilder sb)
        {
            sb.AppendLine("var server = server || {};");

            foreach (IntellisenseObject io in objects)
            {
                if (io.IsEnum) continue;

                string comment = io.Summary ?? "The " + io.Name + " class as defined in " + io.FullName;
                comment = whitespaceTrimmer.Replace(comment, " ");
                sb.AppendLine("/// <summary>" + SecurityElement.Escape(comment) + "</summary>");
                sb.AppendLine("server." + CamelCaseClassName(io.Name) + " = function() {");

                foreach (var p in io.Properties)
                {
                    string type = p.Type.JavaScriptName + (p.Type.IsArray ? "[]" : "");
                    var propertyName = CamelCasePropertyName(p.Name);
                    comment = p.Summary ?? "The " + p.Name + " property as defined in " + io.FullName;
                    comment = whitespaceTrimmer.Replace(comment, " ");
                    sb.AppendLine("\t/// <field name=\"" + propertyName + "\" type=\"" + type + "\">" +
                                  SecurityElement.Escape(comment) + "</field>");
                    sb.AppendLine("\tthis." + propertyName + " = " + p.Type.JavaScripLiteral + ";");
                }

                sb.AppendLine("};");
                sb.AppendLine();
            }
        }

        internal static void WriteTypeScript(IEnumerable<IntellisenseObject> objects, StringBuilder sb, string file = null)
        {
            bool extraLineFeed = false;
            StringBuilder references = new StringBuilder();

            foreach (var ns in objects.GroupBy(o => o.Namespace))
            {
                sb.AppendFormat("declare module {0} {{\r\n", ns.Key);

                foreach (IntellisenseObject io in ns)
                {
                    if (!string.IsNullOrEmpty(io.Summary))
                        sb.AppendLine("\t/** " + whitespaceTrimmer.Replace(io.Summary, "") + " */");
                    if (io.IsEnum)
                    {
                        sb.AppendLine("\tenum " + CamelCaseClassName(io.Name) + " {");
                        foreach (var p in io.Properties)
                        {
                            WriteTypeScriptComment(p, sb);
                            if (p.InitExpression != null)
                            {
                                sb.AppendLine("\t\t" + CamelCasePropertyName(p.Name) + " = " + CleanEnumInitValue(p.InitExpression) + ",");
                            }
                            else
                            {
                                sb.AppendLine("\t\t" + CamelCasePropertyName(p.Name) + ",");
                            }
                        }
                        sb.AppendLine("\t}");
                    }
                    else
                    {
                        sb.Append("\tinterface ").Append(CamelCaseClassName(io.Name)).Append(" ");
                        WriteTSInterfaceDefinition(sb, "\t", io.Properties);
                        sb.AppendLine();

                        if (file != null)
                        {
                            foreach (var reference in io.References.SkipWhile(r => r == file))
                            {
                                references.Insert(0, string.Format(CultureInfo.InvariantCulture, "/// <reference path=\"{0}\" />\r\n", FileHelpers.RelativePath(file, reference)));
                            }

                            if (references.Length > 0)
                            {
                                if (!extraLineFeed)
                                {
                                    references.AppendLine();
                                    extraLineFeed = true;
                                }

                                sb.Insert(0, references);
                                references.Clear();
                            }
                        }
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

        private static void WriteTSInterfaceDefinition(StringBuilder sb, string prefix,
            IEnumerable<IntellisenseProperty> props)
        {
            sb.AppendLine("{");

            foreach (var p in props)
            {
                WriteTypeScriptComment(p, sb);
                sb.AppendFormat("{0}\t{1}: ", prefix, CamelCasePropertyName(p.Name));

                if (p.Type.IsKnownType) sb.Append(p.Type.TypeScriptName);
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
    }
}