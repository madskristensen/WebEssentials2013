using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Html.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions.Html
{
    public abstract class StaticListCompletion : IHtmlCompletionListProvider
    {
        private readonly IReadOnlyDictionary<string, IEnumerable<string>> values;
        private static ReadOnlyCollection<HtmlCompletion> _empty = new ReadOnlyCollection<HtmlCompletion>(new HtmlCompletion[0]);

        protected static ReadOnlyCollection<HtmlCompletion> Empty { get { return _empty; } }

        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }
        ///<summary>Gets the property that serves as a key to look up completion values against.</summary>
        protected abstract string KeyProperty { get; }

        protected StaticListCompletion(Dictionary<string, IEnumerable<string>> values)
        {
            this.values = values;
        }

        ///<summary>Creates a collection of HTML completion items from a list of static values.</summary>
        protected static IEnumerable<string> Values(params string[] staticValues)
        {
            return staticValues;
        }

        ///<summary>Creates a collection of HTML completion items from a list of static values.</summary>
        protected static IEnumerable<string> Values(IEnumerable<string> staticValues)
        {
            return staticValues;
        }

        public virtual IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            IEnumerable<string> result;
            var attr = context.Element.GetAttribute(KeyProperty);
            if (attr == null)
                return Empty;

            if (values.TryGetValue(attr.Value, out result))
            {
                return result.Select(s => new SimpleHtmlCompletion(s, context.Session)).ToList<HtmlCompletion>();
            }

            return Empty;
        }
    }
}