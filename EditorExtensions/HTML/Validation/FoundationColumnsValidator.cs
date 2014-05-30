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
    public class FoundationColumnsValidatorProvider : BaseHtmlElementValidatorProvider<FoundationColumnsValidator>
    { }

    public class FoundationColumnsValidator : BaseValidator
    {
        private static string _errorRowMissing = "Foundation: When using \"{0}\", you must also specify the class \"row\" on the parent element.";
        private static string _errorOver12Columns = "Foundation: \"{0}\" - If you define more than 12 columns, the sum should be a multiple of 12. For examples: http://foundation.zurb.com/docs/components/grid.html";
        private static string _errorUnder12Columns = "Foundation: \"{0}\" - When declaring less then 12 columns, the last column need the 'end' class element.";

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (!WESettings.Instance.Html.EnableFoundationValidation)
                return results;

            var elementClasses = element.GetAttribute("class");
            bool useNameColumn;
            bool useNameColumns;

            if (elementClasses == null)
            {
                return results;
            }
            else
            {
                // 'column' and 'columns' are allowed... unfortunately for this validator: lot of duplicate checks
                useNameColumn = elementClasses.Value.Split(' ').Any(x => x.Contains("column"));
                useNameColumns = elementClasses.Value.Split(' ').Any(x => x.Contains("columns"));

                // No columns class, exit
                if (!useNameColumn && !useNameColumns)
                    return results;
            }

            var columnNameUsed = useNameColumn ? "column" : "columns";

            // Foundation grid system require a direct parent <div class='row ... to work
            if (IsParentDivElementMissingRowClass(element))
            {
                int index = element.Attributes.IndexOf(elementClasses);
                string error = string.Format(CultureInfo.CurrentCulture, _errorRowMissing, columnNameUsed, "");

                results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
            }

            // Check for number of columns...
            string[] columnSizeClasses = new string[] { "small-", "medium-", "large-" };
            var classList = columnSizeClasses.Where(x => elementClasses.Value.Split(' ').Any(y => y.StartsWith(x, StringComparison.Ordinal)));

            // ...only if the doesn't contains '[size]-centered' or '[size]-uncentered' elements
            if (classList.Any())
            {
                string[] columnSizeCenteredClasses = new string[] { "small-centered", "medium-centered", "large-centered",
                                                                    "small-uncentered", "medium-uncentered", "large-uncentered"};
                bool containCenteredElement = columnSizeCenteredClasses.Any(x => elementClasses.Value.Split(' ').Any(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)));

                if (containCenteredElement)
                    return results;
            }

            foreach (var columnSize in classList)
            {
                var sumColumnsCurrentRow = GetSumOfColumns(element, columnSize);

                // It's OK to use < 12 columsn only if the end class is there
                if (sumColumnsCurrentRow < 12 && !IsLastColumnsContainEndClass(element))
                {
                    var index = element.Attributes.IndexOf(elementClasses);
                    var error = string.Format(CultureInfo.CurrentCulture, _errorUnder12Columns, columnSize);

                    results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
                }

                if (sumColumnsCurrentRow > 12 && sumColumnsCurrentRow % 12 != 0)
                {
                    var index = element.Attributes.IndexOf(elementClasses);
                    var error = string.Format(CultureInfo.CurrentCulture, _errorOver12Columns, columnSize);

                    results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
                }
            }

            return results;
        }

        private static bool IsLastColumnsContainEndClass(ElementNode element)
        {
            return element.Parent.Children
                    .Where(x => x.HasAttribute("class"))
                    .Where(x => x.GetAttribute("class").Value.Contains("columns"))
                    .Last()
                    .GetAttribute("class").Value.Contains("end");
        }

        private static int GetSumOfColumns(ElementNode element, string columnSize)
        {
            var columnFilter = columnSize;
            var columnFilterOffset = string.Format(CultureInfo.CurrentCulture, "{0}offset-", columnFilter);

            var sumOfColumns = element.Parent.Children
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
            bool isRowClassPresent = false;

            if (element.Parent == null || string.IsNullOrEmpty(element.Parent.Name))
                return false; // Don't want false alert so better to suppose that it's a partial view of the 'row' class on the parent view.

            var classNames = element.Parent.GetAttribute("class");
            if (classNames != null && classNames.Value.Split(' ').Any(x => x.Equals("row")))
                isRowClassPresent = true;

            if (isRowClassPresent)
            {
                return false;
            }
            else
            {
                if (element.Parent.Name == "html")
                {
                    // Now at the top and no row class on this element. Confirm, it's missing. 
                    return true;
                }

                // For the grid system of Foundation, the row class must be on the direct parent element.
                return true;
            }
        }
    }
}
