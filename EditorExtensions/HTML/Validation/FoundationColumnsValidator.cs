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
        private static string _onlyOneColumnWithoutSizeAllowed = "Foundation: When declaring column without size, only one element with the class 'column' (or 'columns') is allowed for a parent 'row' element.";

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (!WESettings.Instance.Html.EnableFoundationValidation)
                return results;

            var elementClasses = element.GetAttribute("class");

            if (elementClasses == null)
                return results;

            string[] classes = elementClasses.Value.Split(' ');
            bool useNameColumn = classes.Any(x => x.Contains("column"));

            // No columns class, exit
            if (!useNameColumn && !classes.Any(x => x.Contains("columns")))
                return results;

            // Foundation grid system require a direct parent <div class='row ... to work
            if (IsParentDivElementMissingRowClass(element))
                results.AddAttributeError(element,
                                          string.Format(CultureInfo.CurrentCulture, _errorRowMissing, useNameColumn ? "column" : "columns"),
                                          HtmlValidationErrorLocation.AttributeValue,
                                          element.Attributes.IndexOf(elementClasses));

            // Check for number of columns...
            var classList = new[] { "small-", "medium-", "large-" }
                           .Where(x => classes.Any(y => y.StartsWith(x, StringComparison.Ordinal)));

            if (classList.Any())
            {
                // ...only if the doesn't contains '[size]-centered' or '[size]-uncentered' elements
                if (GetCenteredElement(classes))
                    return results;
            }
            // Ok to not have size class only if there is only one column defined (that will be 100%)
            else if (!IsParentDivContainOnlyOneChildColumnElement(element))
                results.AddAttributeError(element,
                                          _onlyOneColumnWithoutSizeAllowed,
                                          HtmlValidationErrorLocation.AttributeValue,
                                          element.Attributes.IndexOf(elementClasses));

            foreach (var columnSize in classList)
            {
                var sumColumnsCurrentRow = GetSumOfColumns(element, columnSize);
                var index = element.Attributes.IndexOf(elementClasses);

                if (sumColumnsCurrentRow == 12)
                    continue;
                // It's OK to use < 12 columsn only if the end class is there
                else if (sumColumnsCurrentRow < 12)
                {
                    if (!IsLastColumnsContainEndClass(element))
                        results.AddAttributeError(element,
                                                  string.Format(CultureInfo.CurrentCulture, _errorUnder12Columns, columnSize),
                                                  HtmlValidationErrorLocation.AttributeValue,
                                                  index);
                }
                else if (sumColumnsCurrentRow % 12 != 0)
                    results.AddAttributeError(element,
                                              string.Format(CultureInfo.CurrentCulture, _errorOver12Columns, columnSize),
                                              HtmlValidationErrorLocation.AttributeValue,
                                              index);
            }

            return results;
        }

        private static bool GetCenteredElement(string[] classes)
        {
            return new[] { "small-centered", "medium-centered", "large-centered", "small-uncentered",
                           "medium-uncentered", "large-uncentered"}
                           .Any(x => classes.Any(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)));
        }

        private static bool IsLastColumnsContainEndClass(ElementNode element)
        {
            return element.Parent.Children
                  .Where(x => x.HasAttribute("class") && x.GetAttribute("class").Value.Contains("columns"))
                  .Last().GetAttribute("class").Value.Contains("end");
        }

        private static int GetSumOfColumns(ElementNode element, string columnSize)
        {
            return element.Parent.Children
                  .Where(x => x.HasAttribute("class") && x.GetAttribute("class").Value.Contains(columnSize))
                  .Select(x => x.GetAttribute("class").Value.Split(' '))
                  .SelectMany(x => x)
                  .Where(x => x.StartsWith(columnSize, StringComparison.CurrentCulture) &&
                             !x.Contains("push") && !x.Contains("pull"))
                  .Sum(x => Int32.Parse(x.Replace(columnSize + "offset-", string.Empty)
                                         .Replace(columnSize, string.Empty),
                                          CultureInfo.CurrentCulture));
        }

        private static bool IsParentDivElementMissingRowClass(ElementNode element)
        {
            if (element.Parent == null || string.IsNullOrEmpty(element.Parent.Name))
                return false; // Don't want false alert so better to suppose that it's a partial view of the 'row' class on the parent view.

            var classNames = element.Parent.GetAttribute("class");

            return classNames == null || !classNames.Value.Split(' ').Any(x => x.Equals("row"));
        }

        private static bool IsParentDivContainOnlyOneChildColumnElement(ElementNode element)
        {
            return element.Parent.Children
                  .Where(x => x.HasAttribute("class") &&
                             (x.GetAttribute("class").Value.Contains("column") ||
                              x.GetAttribute("class").Value.Contains("columns")))
                  .Count() == 1;
        }
    }
}
