using System;
using System.Linq;
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

            foreach (IntellisenseObject io in objects)
            {
                sb.AppendLine("server." + io.Name + " = function()  {");

                foreach (var p in io.Properties)
                {
										string value = p.Type.GetJavaScriptName() + (p.Type.IsArray ? "[]" : "" );
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
						foreach( var ns in objects.GroupBy( o => o.Namespace ) ) {
							sb.AppendFormat("declare module {0} {{\r\n", ns.Key);

							foreach (IntellisenseObject io in ns)
							{
									sb.Append("\tinterface ").Append( io.Name ).Append( " " );
									WriteTSInterfaceDefinition( sb, "\t", io.Properties );
									sb.AppendLine();
							}

							sb.AppendLine("}");
						}
        }

				private static void WriteTSInterfaceDefinition( StringBuilder sb, string prefix, IEnumerable<IntellisenseProperty> props ) {
					sb.AppendLine( "{" );
					
					foreach ( var p in props ) {
						sb.AppendFormat( "{0}\t{1}: ", prefix, p.Name );
						
						if ( p.Type.IsPrimitive() ) sb.Append( p.Type.GetJavaScriptName() );
						else if ( !string.IsNullOrEmpty( p.Type.ClientSideReferenceName ) ) sb.Append( p.Type.ClientSideReferenceName );
						else {
							if ( p.Type.Shape == null ) sb.Append( "any" );
							else WriteTSInterfaceDefinition( sb, prefix + "\t", p.Type.Shape );
						}
						if ( p.Type.IsArray ) sb.Append( "[]" );

						sb.AppendLine( ";" );
					}

					sb.Append( prefix ).Append( "}" );
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

			public bool IsPrimitive() { return GetJavaScriptName() != "Object"; }

			public string GetJavaScriptName() {
				switch ( CodeName.ToLower() ) {
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

				if ( CodeName.Contains( "System.Collections" ) || CodeName.Contains( "[]" ) || CodeName.Contains( "Array" ) )
					return "Array";

				return "Object";
			}
		}
}
