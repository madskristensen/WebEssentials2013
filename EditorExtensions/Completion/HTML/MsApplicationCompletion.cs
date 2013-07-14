using Microsoft.Html.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    [HtmlCompletionProvider(CompletionType.Values, "meta", "content")]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    public class MsApplicationCompletion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        private List<string> _booleanNames = new List<string>()
        {
            "msapplication-allowdomainapicalls",
            "msapplication-allowdomainmetatags"
        };

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            var result = new List<HtmlCompletion>();
            var attr = context.Element.GetAttribute("name");

            if (attr != null && _booleanNames.Contains(attr.Value.ToLowerInvariant()))
            {
                result.Add(new SimpleHtmlCompletion("false"));
                result.Add(new SimpleHtmlCompletion("true"));
            }

            return result;
        }
    }
}
