using System.Collections.Generic;
using System.Globalization;

namespace MadsKristensen.EditorExtensions
{
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

        public bool IsDictionary { get; set; }

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

        public bool IsKnownType
        {
            get { return TypeScriptName != "any"; }
        }

        public string JavaScriptName
        {
            get { return GetTargetName(this.CodeName, true); }
        }

        public string TypeScriptName
        {
            get
            {
                if (IsDictionary) return GetKVPTypes();
                return GetTargetName(this.CodeName, false);
            }
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


        private string GetTargetName(string codeName, bool js)
        {
            var t = codeName.ToLowerInvariant().TrimEnd('?');
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
            return js ? "Object" : GetComplexTypeScriptName();
        }

        private string GetComplexTypeScriptName()
        {
            return ClientSideReferenceName ?? "any";
        }

        private string GetKVPTypes()
        {
            var type = CodeName.ToLowerInvariant().TrimEnd('?');
            var types = type.Split('<', '>')[1].Split(',');
            string keyType = GetTargetName(types[0].Trim(), false);

            if (keyType != "string" || keyType != "number")
            { // only string or number are allowed for keys
                keyType = "string";
            }

            string valueType = GetTargetName(types[1].Trim(), false);

            return string.Format(CultureInfo.CurrentCulture, "{{ [index: {0}]: {1} }}", keyType, valueType);
        }
    }
}