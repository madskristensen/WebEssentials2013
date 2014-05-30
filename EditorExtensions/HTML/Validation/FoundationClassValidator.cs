using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private static string _errorMissingColumns = "Foundation: When using \"small-#\", \"medium-#\" or \"large-#\", you must also specify the class \"columns\".";
        private static string _errorMissingSize = "Foundation: When using \"columns\", you must also specify the class \"small-#\", \"medium-#\" or \"large-#\".";

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

        public static bool ColumnPairElementsOk(string input)
        {
            string[] columnClasses = new string[] { "columns", "column" };
            string[] columnSizeClasses = new string[] { "small-", "medium-", "large-" };

            var containColumnClass = input.Split(' ').Any(x => columnClasses.Contains(x));
            var containSizeClass = columnSizeClasses.Any(x => input.Split(' ')
                                                    .Where(toExclude => !toExclude.Contains("block-grid"))
                                                    .Any(y => y.StartsWith(x, StringComparison.Ordinal)));

            // If both are there, or both are missing it's OK
            if ((containColumnClass && containSizeClass) || (!containColumnClass && !containSizeClass))
                return true;
            else
                return false;
        }
    }
}
