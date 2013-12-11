using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace MadsKristensen.EditorExtensions.Validation.Html
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class AngularMissingAppValidatorProvider : BaseHtmlElementValidatorProvider<AngularMissingAppValidator>
    { }

    public class AngularMissingAppValidator : BaseValidator, IHtmlTreeVisitor
    {
        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();
            AttributeNode attr = element.Attributes.SingleOrDefault(a => a.Name.StartsWith("ng-"));

            if (ShouldIgnore(element, attr))
                return results;

            string error = "Angular: The 'ng-app' attribute is missing on a parent element.";

            if (attr != null)
            {
                int index = element.Attributes.IndexOf(attr);
                results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeName, index);
            }
            else
            {
                results.Add(element, error, HtmlValidationErrorLocation.ElementName);
            }

            return results;
        }

        private bool ShouldIgnore(ElementNode element, AttributeNode attr)
        {
            // Ignore doctype
            if (element.IsDocType())
                return true;

            // Ignore everything bug <ng-*> elements and ng-* attributes
            if (!element.Name.StartsWith("ng-") && (attr == null || attr.Name == "ng-app"))
                return true;

            // Ignore if <html> isn't present in the document (probably a partial or similar)
            if (!HasHtmlElement(element))
                return true;

            // Follow the parent chain to find any ng-app attributes
            return IsParentApp(element);
        }

        private bool HasHtmlElement(ElementNode element)
        {
            var list = new HashSet<string>();

            element.Root.Accept(this, list);

            return list.Count > 0;
        }

        private bool IsParentApp(ElementNode element)
        {
            if (element.GetAttribute("ng-app") != null)
                return true;

            if (element.Parent != null)
                return IsParentApp(element.Parent);

            return false;
        }

        public bool Visit(ElementNode element, object parameter)
        {
            if (element.Name == "html")
            {
                var list = (HashSet<string>)parameter;
                list.Add(string.Empty);
            }

            return true;
        }
    }
}
