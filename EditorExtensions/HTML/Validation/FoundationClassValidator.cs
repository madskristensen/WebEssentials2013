using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class FoundationClassValidatorProvider : BaseHtmlElementValidatorProvider<FoundationClassValidator>
    { }

    public class FoundationClassValidator : BaseValidator
    {
        private static string _errorMissingColumns = "When using \"small-#\", \"medium-#\" or \"large-#\", you must also specify the class \"columns\".";
        private static string _errorMissingSize = "When using \"columns\", you must also specify the class \"small-#\", \"medium-#\" or \"large-#\".";

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (!WESettings.Instance.Html.EnableFoundationValidation)
                return results;

            var classNames = element.GetAttribute("class");

            if (classNames == null)
                return results;

            if (!ColumnPairElementsOk(classNames.Value))
            {
                int index = element.Attributes.IndexOf(classNames);
                var specificErrorMessage = classNames.Value.Contains("column") ? _errorMissingSize : _errorMissingColumns;

                results.AddAttributeError(element, specificErrorMessage, HtmlValidationErrorLocation.AttributeValue, index);
            }

            return results;
        }

        private bool ColumnPairElementsOk(string input)
        {
            if (input.Contains("columns") || input.Contains("column")) {

                if (input.Contains("small-") || input.Contains("medium-") || input.Contains("large-"))
                {
                    // Both elements are there
                    return true;
                }

                // Size is missing
                return false;
            }
            else
            {
                if (input.Contains("small-") || input.Contains("medium-") || input.Contains("large-"))
                {
                    // Size w/o columns. 
                    return false;
                }
            }
            
            // No columns elements. OK
            return true;
        }
    }
}
