using MadsKristensen.EditorExtensions.Settings;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;

namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class FoundationColumnsValidatorProvider : BaseHtmlElementValidatorProvider<FoundationColumnsValidator>
    { }

    public class FoundationColumnsValidator : BaseValidator
    {
        private static string _errorRowMissing = "Foundation: When using \"{0}\", you must also specify the class \"row\" on the parent element.";

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (!WESettings.Instance.Html.EnableFoundationValidation)
                return results;

            var elementClasses = element.GetAttribute("class");
            bool useNameColumn;
            bool useNameColumns;

            // 'column' and 'columns' are allowed... unfortunately for this validator: lot of duplicate checks
            if (elementClasses == null)
            {
                return results;
            }
            else
            {
                useNameColumn = elementClasses.Value.Split(' ').Any(x => x.Contains("column"));
                useNameColumns = elementClasses.Value.Split(' ').Any(x => x.Contains("columns"));

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

            return results;
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
