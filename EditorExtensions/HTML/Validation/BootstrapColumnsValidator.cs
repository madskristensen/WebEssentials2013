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
        private static string _errorRowMissing = "Bootstrap: When using \"{0}\", you must also specify the class \"row\" on a parent element.";

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (!WESettings.Instance.Html.EnableBootstrapValidation)
                return results;

            var elementClasses = element.GetAttribute("class");

            if (elementClasses == null || !elementClasses.Value.Split(' ').Any(x => x.StartsWith("col-", StringComparison.CurrentCulture)))
                return results;

            // Bootstrap grid system require a parent <div class='row ... to work
            if (IsParentDivElementMissingRowClass(element))
                results.AddAttributeError(element,
                                          string.Format(CultureInfo.CurrentCulture, _errorRowMissing, elementClasses.Value.Split(' ').Where(x => x.StartsWith("col-", StringComparison.CurrentCulture)).First()),
                                          HtmlValidationErrorLocation.AttributeValue,
                                          element.Attributes.IndexOf(elementClasses));

            return results;
        }

        private static bool IsParentDivElementMissingRowClass(ElementNode element)
        {
            if (element.Parent == null)
                return false; // Don't want false alert so better to suppose that it's a partial view of the 'row' class on the parent view.

            var classNames = element.Parent.GetAttribute("class");

            if (classNames != null && classNames.Value.Split(' ').Any(x => x.Equals("row")))
                return false;

            // Now at the top and no row class on this element. Confirm, it's missing. 
            if (element.Parent.Name == "html")
                return true;

            // Check if a parent element have the row class.
            return IsParentDivElementMissingRowClass(element.Parent);
        }
    }
}
