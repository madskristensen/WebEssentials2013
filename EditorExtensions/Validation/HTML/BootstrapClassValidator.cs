using Microsoft.Html.Core;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.Html.Validation;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class BootstrapClassValidatorProvider : BaseHtmlElementValidatorProvider<BootstrapClassValidator>
    { }

    public class BootstrapClassValidator : BaseValidator
    {
        private static string[] _tokens = new[] { "btn", "glyphicon", "alert", "label", "fa" }; // fa is for FontAwesome
        private static string[] _childDependentTokes = new[] { "fa" };
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
                    string error = string.Format(_error, offender, token);

                    results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
                }
            }

            return results;
        }

        private List<string> GetIfTokenIsDependent(string token, ElementNode element)
        {
            List<string> childrenClassNames = null;

            if (_childDependentTokes.Any(tk => token.StartsWith(tk)))
            {
                childrenClassNames = new List<string>();

                foreach (var child in element.Children)
                {
                    childrenClassNames.Add(child.GetAttribute("class") == null ? child.GetAttribute("class").Value : "");
                }
            }
            return childrenClassNames;
        }

        private static bool IsCorrect(string input, string token, List<string> childrenClassNames = null)
        {
            if (input.Contains(token + "-") &&
                 !(input.Contains(token + " ") || input.EndsWith(token) || IsWhitelisted(input, token)) &&
                ((childrenClassNames != null && !childrenClassNames.All(cn => cn.Contains(token + " ") || cn.EndsWith(token))) || childrenClassNames == null))
                return false;

            return true;
        }

        private static string GetOffendingClassName(string input, string token)
        {
            string[] classes = input.Split(' ');
            return classes.FirstOrDefault(c => c.StartsWith(token + "-"));
        }

        private static bool IsWhitelisted(string input, string token)
        {
            return _whitelist.ContainsKey(token) && _whitelist[token].Any(s => input.Contains(s));
        }
    }
}
