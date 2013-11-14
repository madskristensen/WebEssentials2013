using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions.Validation.HTML
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class BootstrapClassValidatorProvider : BaseHtmlElementValidatorProvider<BootstrapClassValidator>
    { }

    public class BootstrapClassValidator : BaseValidator
    {
        private static string[] _tokens = new[] { "btn", "glyphicon", "alert", "label", "fa" }; // fa is for FontAwesome

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
                    results.AddAttributeError(element, "You must also specify the class \"" + token + "\"", HtmlValidationErrorLocation.AttributeValue, index);
                }
            }

            return results;
        }

        private bool IsCorrect(string input, string token)
        {
            if (input.Contains(token + "-") &&
                !(input.Contains(token + " ") || input.EndsWith(token)))
                return false;

            return true;
        }
    }
}
