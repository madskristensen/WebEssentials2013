using Microsoft.VisualStudio.Web.PasteJson;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IPasteJsonCodeGenerator))]
    [ExportMetadata("CodeGeneratorType", "TypeScript")]
    internal class CSharpCodeGenerator : IPasteJsonCodeGenerator
    {
        private Regex _validIdentifierRegex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");

        public string GenerateStartClass(string className)
        {
            return string.Format("interface {0}", className) + Environment.NewLine + "{";
        }

        public string GenerateProperty(PasteJsonUtil.LangIndependentType langIndependentType, string propertyName)
        {
            return string.Format("{1}: {0};", GeneratePropertyTypeName(langIndependentType), propertyName);
        }

        public string GenerateObjectProperty(string typeOfObject, string propertyName)
        {
            return string.Format("{1}: {0};", typeOfObject, propertyName);
        }

        public string GenerateArrayProperty(int dimensions, string typeOfArray, string propertyName)
        {
            if (dimensions > 0)
            {
                string mutliDimension = GetArrayDeclaration(dimensions);
                return string.Format("{2}: {0}{1};", typeOfArray, mutliDimension, propertyName);
            }
            return null;
        }

        public string GenerateArrayProperty(int dimensions, PasteJsonUtil.LangIndependentType langIndependentType, string propertyName)
        {
            if (dimensions > 0)
            {
                string mutliDimension = GetArrayDeclaration(dimensions);
                return string.Format("{2}: {0}{1};", GeneratePropertyTypeName(langIndependentType), mutliDimension, propertyName);
            }
            return null;
        }

        private string GetArrayDeclaration(int dimension)
        {
            StringBuilder arrayDeclaration = new StringBuilder();
            for (int loop = 1; loop <= dimension; loop++)
            {
                arrayDeclaration.Append(GeneratePropertyTypeName(PasteJsonUtil.LangIndependentType.Array));
            }
            return arrayDeclaration.ToString();
        }

        public string GenerateEndClass(string className)
        {
            return "}";
        }

        public string MakeValidName(string name)
        {
            return _validIdentifierRegex.Replace(name, "");
        }

        private string GeneratePropertyTypeName(PasteJsonUtil.LangIndependentType langIndependentType)
        {
            string returnType = string.Empty;
            Debug.Assert(langIndependentType != PasteJsonUtil.LangIndependentType.Object, "I should not be expecting Object");

            switch (langIndependentType)
            {
                case PasteJsonUtil.LangIndependentType.Array:
                    returnType = "[]";
                    break;
                case PasteJsonUtil.LangIndependentType.Boolean:
                    returnType = "bool";
                    break;
                case PasteJsonUtil.LangIndependentType.Date:
                    returnType = "Date";
                    break;
                case PasteJsonUtil.LangIndependentType.Double:
                    returnType = "number";
                    break;
                case PasteJsonUtil.LangIndependentType.Float:
                    returnType = "number";
                    break;
                case PasteJsonUtil.LangIndependentType.Integer:
                    returnType = "number";
                    break;
                case PasteJsonUtil.LangIndependentType.Long:
                    returnType = "number";
                    break;
                case PasteJsonUtil.LangIndependentType.NullableBoolean:
                    returnType = "bool";
                    break;
                case PasteJsonUtil.LangIndependentType.NullableDate:
                    returnType = "Date";
                    break;
                case PasteJsonUtil.LangIndependentType.NullableDouble:
                    returnType = "number";
                    break;
                case PasteJsonUtil.LangIndependentType.NullableFloat:
                    returnType = "number";
                    break;
                case PasteJsonUtil.LangIndependentType.NullableInteger:
                    returnType = "number";
                    break;
                case PasteJsonUtil.LangIndependentType.NullableLong:
                    returnType = "number";
                    break;
                case PasteJsonUtil.LangIndependentType.NullOrUndefined:
                    returnType = "any";
                    break;
                case PasteJsonUtil.LangIndependentType.String:
                    returnType = "string";
                    break;
                case PasteJsonUtil.LangIndependentType.Unknown:
                    returnType = "any";
                    break;
                case PasteJsonUtil.LangIndependentType.Uri:
                    returnType = "string";
                    break;
                default:
                    Debug.Assert(true, "Property Type:" + langIndependentType.ToString() + " is not supported !!");
                    break;
            }
            return returnType;
        }
    }
}
