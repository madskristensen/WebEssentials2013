using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class BootstrapClassValidatorProvider : BaseHtmlElementValidatorProvider<BootstrapClassValidator>
    { }

    public class BootstrapClassValidator : BaseValidator
    {
        private static string[] _tokens = new[] { "btn", "glyphicon", "alert", "label", "fa" }; // fa is for FontAwesome
        private static string _error = "When using \"{0}\", you must also specify the class \"{1}\".";

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();
            var classNames = element.GetAttribute("class");

            if (classNames == null)
                return results;

            foreach (string token in _tokens)
            {
                if (!IsCorrect(classNames.Value, token))
                {
                    int index = element.Attributes.IndexOf(classNames);
                    string offender = GetOffendingClassName(classNames.Value, token);
                    string error = string.Format(_error, offender, token);

                    results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
                }
            }

            return results;
        }

        private static bool IsCorrect(string input, string token)
        {
            if (input.Contains(token + "-") &&
                !(input.Contains(token + " ") || input.EndsWith(token)))
                return false;

            return true;
        }

        private static string GetOffendingClassName(string input, string token)
        {
            string[] classes = input.Split(' ');
            return classes.FirstOrDefault(c => c.StartsWith(token + "-"));
        }
    }
}
