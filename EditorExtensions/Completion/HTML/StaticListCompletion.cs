using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Html.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    public abstract class StaticListCompletion : IHtmlCompletionListProvider
    {
        private readonly IReadOnlyDictionary<string, IList<HtmlCompletion>> values;
        private static ReadOnlyCollection<HtmlCompletion> _empty = new ReadOnlyCollection<HtmlCompletion>(new HtmlCompletion[0]);

        protected static ReadOnlyCollection<HtmlCompletion> Empty { get { return _empty; } }

        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }
        ///<summary>Gets the property that serves as a key to look up completion values against.</summary>
        protected abstract string KeyProperty { get; }

        protected StaticListCompletion(Dictionary<string, IList<HtmlCompletion>> values)
        {
            this.values = values;
        }

        ///<summary>Creates a collection of HTML completion items from a list of static values.</summary>
        protected static ReadOnlyCollection<HtmlCompletion> Values(params string[] staticValues)
        {
            return new ReadOnlyCollection<HtmlCompletion>(Array.ConvertAll(staticValues, s => new SimpleHtmlCompletion(s)));
        }

        ///<summary>Creates a collection of HTML completion items from a list of static values.</summary>
        protected static ReadOnlyCollection<HtmlCompletion> Values(IEnumerable<string> staticValues)
        {
            return new ReadOnlyCollection<HtmlCompletion>(staticValues.Select(s => new SimpleHtmlCompletion(s)).ToList<HtmlCompletion>());
        }

        public IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            IList<HtmlCompletion> result;
            var attr = context.Element.GetAttribute(KeyProperty);
            if (attr == null)
                return Empty;

            values.TryGetValue(attr.Value, out result);
            return result ?? Empty;
        }
    }
}