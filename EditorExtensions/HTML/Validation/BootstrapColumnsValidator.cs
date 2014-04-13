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
    public class BootstrapColumnsValidatorProvider : BaseHtmlElementValidatorProvider<BootstrapColumnsValidator>
    { }

    public class BootstrapColumnsValidator : BaseValidator
    {
        private static string _errorRowMissing = "Bootstrap: When using \"{0}\", you must also specify the class \"row\" on the parent div element.";
        private static string _errorInvalidSum = "Bootstrap: Sum of columns of type {0} must equal 12.";

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (!WESettings.Instance.Html.EnableBootstrapValidation)
                return results;

            var elementClasses = element.GetAttribute("class");

            if (elementClasses == null || !elementClasses.Value.Split(' ').Any(x => x.StartsWith("col-", StringComparison.CurrentCulture)))
            {
                return results;
            }

            // Bootstrap grid system require a parent <div class='row ... to work
            if (IsParentDivElementMissingRowClass(element))
            {
                int index = element.Attributes.IndexOf(elementClasses);
                string columnsClass = elementClasses.Value.Split(' ').Where(x => x.StartsWith("col-", StringComparison.CurrentCulture)).First();
                string error = string.Format(CultureInfo.CurrentCulture, _errorRowMissing, columnsClass, "");

                results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
            }

            // Sum of columns size of a row must egal 12. 
            var classList = elementClasses.Value.Split(' ')
                                .Select(x => x.Trim())
                                .Where(x => x.StartsWith("col-", StringComparison.CurrentCulture));
            foreach (var c in classList)
            {
                // Find the type (size) of column
                var columnSize = c.Replace("col-", string.Empty).Substring(0, 2);
                var sumColumnsCurrentRow = GetSumOfColumns(element, columnSize); ;

                if (sumColumnsCurrentRow != 12)
                {
                    int index = element.Attributes.IndexOf(elementClasses);
                    var columnType = string.Format(CultureInfo.CurrentCulture, "col-{0}-*", columnSize);
                    string error = string.Format(CultureInfo.CurrentCulture, _errorInvalidSum, columnType);

                    results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
                }
            }

            return results;
        }

        private static Int32 GetSumOfColumns(ElementNode element, string columnSize)
        {
            var columnFilter = string.Format(CultureInfo.CurrentCulture, "col-{0}-", columnSize);
            var columnFilterOffset = string.Format(CultureInfo.CurrentCulture, "{0}offset-", columnFilter);

            var sumOfColumns = element.Parent.Children
                                .Where(x => x.Name == "div")
                                .Where(x => x.HasAttribute("class"))
                                .Where(x => x.GetAttribute("class").Value.Contains(columnFilter))
                                .Select(x => x.GetAttribute("class").Value.Split(' '))
                                .SelectMany(x => x)
                                .Where(x => x.StartsWith(columnFilter, StringComparison.CurrentCulture))
                                .Where(x => !x.Contains("push") && !x.Contains("pull"))
                                .Sum(x => Int32.Parse(x.Replace(columnFilterOffset, string.Empty)
                                                       .Replace(columnFilter, string.Empty),
                                                       CultureInfo.CurrentCulture));

            return sumOfColumns;
        }

        private static bool IsParentDivElementMissingRowClass(ElementNode element)
        {
            if (element.Parent == null)
                return true; // To review, can cause false positive on some partials view?

            var classNames = element.Parent.GetAttribute("class");
            if (classNames == null)
                return true;

            if (classNames.Value.Split(' ').Any(x => x.Equals("row")))
                return false;

            return true;
        }
    }
}
