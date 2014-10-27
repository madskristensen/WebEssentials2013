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
    public class BootstrapClassValidatorProvider : BaseHtmlElementValidatorProvider<BootstrapClassValidator>
    { }

    public class BootstrapClassValidator : BaseValidator
    {
        private static string[] _tokens = new[] { "btn", "glyphicon", "alert", "label", "fa" }; // fa is for FontAwesome
        private static string[] _childDependentTokens = new[] { "fa" };
        private static string _error = "When using \"{0}\", you must also specify the class \"{1}\".";
        private static Dictionary<string, string[]> _whitelist = new Dictionary<string, string[]>
        {
            { "btn", new [] { "btn-group", "btn-toolbar" } }
        };

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            var results = new ValidationErrorCollection();

            if (!WESettings.Instance.Html.EnableBootstrapValidation)
                return results;

            var classNames = element.GetAttribute("class");

            if (classNames == null)
                return results;

            foreach (string token in _tokens)
            {
                if (IsCorrect(classNames.Value, token, GetIfTokenIsDependent(token, element)))
                    continue;

                results.AddAttributeError(element,
                                          string.Format(CultureInfo.CurrentCulture, _error, GetOffendingClassName(classNames.Value, token), token),
                                          HtmlValidationErrorLocation.AttributeValue,
                                          element.Attributes.IndexOf(classNames));
            }

            return results;
        }

        private static List<string> GetIfTokenIsDependent(string token, ElementNode element)
        {
            if (!_childDependentTokens.Any(tk => token.StartsWith(tk, StringComparison.Ordinal)))
                return null;

            return element.Children.Where(child => child.GetAttribute("class") != null)
                          .Select(child => child.GetAttribute("class").Value).ToList();
        }

        private static bool IsCorrect(string input, string token, List<string> childrenClassNames = null)
        {
            return !(input.Contains(token + "-") &&
                   !(input.Contains(token + " ") || input.EndsWith(token, StringComparison.CurrentCulture) || IsWhitelisted(input, token)) &&
                   ((childrenClassNames != null && !childrenClassNames.All(cn => cn.Contains(token + " ") || cn.EndsWith(token, StringComparison.CurrentCulture))) || childrenClassNames == null));
        }

        private static string GetOffendingClassName(string input, string token)
        {
            return input.Split(' ').FirstOrDefault(c => c.StartsWith(token + "-", StringComparison.CurrentCulture));
        }

        private static bool IsWhitelisted(string input, string token)
        {
            return _whitelist.ContainsKey(token) && _whitelist[token].Any(s => input.Contains(s));
        }
    }
}
