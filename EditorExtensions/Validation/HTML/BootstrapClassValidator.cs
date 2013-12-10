using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
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
            var classNames = element.GetAttribute("class");
            List<string> childrenClassNames = null;

            if (classNames == null)
                return results;

            foreach (string token in _tokens)
            {
                childrenClassNames = GetIfTokenIsDependent(token, element);

                if (!IsCorrect(classNames.Value, token, childrenClassNames))
                {
                    int index = element.Attributes.IndexOf(classNames);
                    string offender = GetOffendingClassName(classNames.Value, token);
                    string error = string.Format(CultureInfo.CurrentCulture, _error, offender, token);

                    results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
                }
            }

            return results;
        }

        private static List<string> GetIfTokenIsDependent(string token, ElementNode element)
        {
            List<string> childrenClassNames = null;

            if (_childDependentTokens.Any(tk => token.StartsWith(tk, StringComparison.Ordinal)))
            {
                childrenClassNames = new List<string>();
                // childrenClassNames = element.Children.Select<ElementNode, string>(child => child.GetAttribute("class") == null ? "" : child.GetAttribute("class").Value).ToList<string>();
                childrenClassNames = element.Children.Where(child => child.GetAttribute("class") != null).Select(child => child.GetAttribute("class").Value).ToList();
            }

            return childrenClassNames;
        }

        private static bool IsCorrect(string input, string token, List<string> childrenClassNames = null)
        {
            if (input.Contains(token + "-") &&
                 !(input.Contains(token + " ") || input.EndsWith(token, StringComparison.CurrentCulture) || IsWhitelisted(input, token)) &&
                ((childrenClassNames != null && !childrenClassNames.All(cn => cn.Contains(token + " ") || cn.EndsWith(token, StringComparison.CurrentCulture))) || childrenClassNames == null))
                return false;

            return true;
        }

        private static string GetOffendingClassName(string input, string token)
        {
            string[] classes = input.Split(' ');
            return classes.FirstOrDefault(c => c.StartsWith(token + "-", StringComparison.CurrentCulture));
        }

        private static bool IsWhitelisted(string input, string token)
        {
            return _whitelist.ContainsKey(token) && _whitelist[token].Any(s => input.Contains(s));
        }
    }
}
