using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Validation.Html
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AngularMissingAppValidatorProvider : BaseHtmlElementValidatorProvider<AngularMissingAppValidator>
    { }

    public class AngularMissingAppValidator : BaseValidator
    {
        private const string _error = "Angular: The 'ng-app' attribute is missing on a parent element.";

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (!WESettings.Instance.Html.EnableAngularValidation)
                return results;

            AttributeNode attr = element.Attributes.SingleOrDefault(a => a.Name.StartsWith("ng-", StringComparison.Ordinal) || a.Name.StartsWith("data-ng-", StringComparison.Ordinal));

            if (ShouldIgnore(element, attr))
                return results;

            if (attr != null)
            {
                int index = element.Attributes.IndexOf(attr);
                results.AddAttributeError(element, _error, HtmlValidationErrorLocation.AttributeName, index);
            }
            else
            {
                results.Add(element, _error, HtmlValidationErrorLocation.ElementName);
            }

            return results;
        }

        private bool ShouldIgnore(ElementNode element, AttributeNode attr)
        {
            // Ignore doctype
            if (element.IsDocType())
                return true;

            // Ignore everything bug <ng-*> elements and ng-* attributes
            if (!element.Name.StartsWith("ng-", StringComparison.Ordinal) && (attr == null || attr.Name == "ng-app" || attr.Name == "data-ng-app"))
                return true;

            // Ignore if <html> isn't present in the document (probably a partial or similar)
            if (!HasHtmlElement(element))
                return true;

            // Follow the parent chain to find any ng-app attributes
            return IsParentNgApp(element);
        }

        private bool HasHtmlElement(ElementNode element)
        {
            if (element.Name == "html")
                return true;

            if (element.Parent != null)
                return HasHtmlElement(element.Parent);

            return false;
        }

        private bool IsParentNgApp(ElementNode element)
        {
            if (element.HasAttribute("ng-app") || element.HasAttribute("data-ng-app") || element.HasClass("ng-app"))
                return true;

            if (element.Parent != null)
                return IsParentNgApp(element.Parent);

            return false;
        }
    }
}
