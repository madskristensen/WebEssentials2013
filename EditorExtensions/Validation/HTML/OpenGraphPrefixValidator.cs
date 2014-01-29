using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Validation.Html
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class OpenGraphPrefixValidatorProvider : BaseHtmlElementValidatorProvider<OpenGraphPrefixValidator>
    { }

    public class OpenGraphPrefixValidator : BaseValidator
    {
        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (element.Name != "meta" || !element.HasAttribute("property"))
                return results;

            AttributeNode property = element.GetAttribute("property");

            if (property.Value.StartsWith("og:", StringComparison.Ordinal))
            {
                ElementNode head = element.Parent;

                if (head != null && head.Name == "head")
                {
                    AttributeNode prefix = head.GetAttribute("prefix");
                    int index = element.Attributes.IndexOf(property);

                    if (prefix == null)
                    {
                        results.AddAttributeError(element, "The 'prefix' attribute on <head> for Open Graph is missing", HtmlValidationErrorLocation.AttributeValue, index);
                    }
                    else if (prefix.Value.IndexOf("og:", StringComparison.Ordinal) == -1)
                    {
                        results.AddAttributeError(element, "To use Open Graph, you must add the value 'og: http://ogp.me/ns#' to 'prefix' in <head>", HtmlValidationErrorLocation.AttributeValue, index);
                    }
                }
            }

            return results;
        }
    }
}
