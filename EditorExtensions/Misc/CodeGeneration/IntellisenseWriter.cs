using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Settings;

namespace MadsKristensen.EditorExtensions
{
    internal static class IntellisenseWriter
    {
        public async static Task Write(IEnumerable<IntellisenseObject> objects, string file)
        {
            StringBuilder sb = new StringBuilder();

            if (Path.GetExtension(file).Equals(".ts", StringComparison.OrdinalIgnoreCase))
                WriteTypeScript(objects, sb, file);
            else
                WriteJavaScript(objects, sb);

            await FileHelpers.WriteAllTextRetry(file, sb.ToString());
        }

        private static string CamelCaseEnumValue(string name)
        {
            if (WESettings.Instance.CodeGen.CamelCaseEnumerationValues)
            {
                name = CamelCase(name);
            }
            return name;
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
            bool hasInheritDefinition = false;

            sb.AppendLine("var server = server || {};");

            foreach (IntellisenseObject io in objects)
            {
                if (io.IsEnum) continue;

                string comment = io.Summary ?? "The " + io.Name + " class as defined in " + io.FullName;

                comment = whitespaceTrimmer.Replace(comment, " ");

                sb.Append("/// <summary>").Append(SecurityElement.Escape(comment)).Append("</summary>").AppendLine();
                sb.Append("server.").Append(CamelCaseClassName(io.Name)).Append(" = function() {").AppendLine();

                foreach (var p in io.Properties)
                {
                    string type = p.Type.JavaScriptName + (p.Type.IsArray ? "[]" : "");
                    var propertyName = CamelCasePropertyName(p.Name);

                    comment = p.Summary ?? "The " + p.Name + " property as defined in " + io.FullName;
                    comment = whitespaceTrimmer.Replace(comment, " ");

                    sb.Append("\t/// <field name=\"").Append(propertyName).Append("\" type=\"").Append(type).Append("\">")
                      .Append(SecurityElement.Escape(comment)).Append("</field>").AppendLine();
                    sb.Append("\tthis.").Append(propertyName).Append(" = ").Append(p.Type.JavaScripLiteral + ";")
                      .AppendLine();
                }

                sb.AppendLine("};").AppendLine();

                if (!string.IsNullOrEmpty(io.BaseName))
                {
                    if (!hasInheritDefinition)
                    {
                        sb.Insert(0, GetInheritMethod());

                        hasInheritDefinition = true;
                    }

                    sb.Append("inherits(").Append(io.Name).Append(", ").Append(io.BaseName).Append(");");
                }
            }
        }

        private static string GetInheritMethod()
        {
            // Taken from http://blog.slaks.net/2013-09-03/traditional-inheritance-in-javascript/

            return @"function inherits(subConstructor, superConstructor) {
    var proto = Object.create(
        superConstructor.prototype,
        {
            ""constructor"": { 
                configurable: true,
                enumerable: false,
                writable: true,
                value: subConstructor
            }
        }
    );
    Object.defineProperty(subConstructor, ""prototype"",  { 
        configurable: true,
        enumerable: false,
        writable: true,
        value: proto
    });
}" + Environment.NewLine + Environment.NewLine;
        }

        internal static void WriteTypeScript(IEnumerable<IntellisenseObject> objects, StringBuilder sb, string file = null)
        {
            if (WESettings.Instance.CodeGen.AddTypeScriptReferencePath && !string.IsNullOrEmpty(file))
            {
                var references = objects.SelectMany(io => io.References.Where(r => r != file)).Distinct().ToList();

                foreach (var reference in references)
                    sb.AppendFormat("/// <reference path=\"{0}\" />\r\n", FileHelpers.RelativePath(file, reference));

                if (references.Count > 0) sb.AppendLine();
            }

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
                                sb.AppendLine("\t\t" + CamelCaseEnumValue(p.Name) + " = " + CleanEnumInitValue(p.InitExpression) + ",");
                            }
                            else
                            {
                                sb.AppendLine("\t\t" + CamelCaseEnumValue(p.Name) + ",");
                            }
                        }

                        sb.AppendLine("\t}");
                    }
                    else
                    {
                        sb.Append("\tinterface ").Append(CamelCaseClassName(io.Name)).Append(" ");

                        if (!string.IsNullOrEmpty(io.BaseName))
                        {
                            sb.Append("extends ");

                            if (!string.IsNullOrEmpty(io.BaseNamespace) && io.BaseNamespace != io.Namespace)
                                sb.Append(io.BaseNamespace).Append(".");

                            sb.Append(io.BaseName).Append(" ");
                        }

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