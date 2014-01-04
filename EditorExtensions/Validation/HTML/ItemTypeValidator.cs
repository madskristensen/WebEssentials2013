using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Validation.Html
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class ItemTypeValidatorProvider : BaseHtmlElementValidatorProvider<ItemTypeValidator>
    { }

    public class ItemTypeValidator : BaseValidator
    {
        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (element.IsDocType())
                return results;

            for (int i = 0; i < element.Attributes.Count; i++)
            {
                var attr = element.Attributes[i];

                if (attr.Name == "itemtype" && attr.HasValue() && !attr.Value.Contains("@"))
                {
                    if (!Uri.IsWellFormedUriString(attr.Value, UriKind.Absolute))
                    {
                        results.AddAttributeError(element, String.Format(CultureInfo.CurrentCulture, "The value of {0} must be an absolute URI", attr.Name), HtmlValidationErrorLocation.AttributeValue, i);
                        break;
                    }
                }
            }

            return results;
        }
    }
}
