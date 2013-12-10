using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Html.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    public abstract class StaticListCompletion : IHtmlCompletionListProvider
    {
        public CompletionType CompletionType
        {
            get { return CompletionType.Values; }
        }

        protected StaticListCompletion(Dictionary<string, IList<HtmlCompletion>> values)
        {
            this.values = values;
        }

        ///<summary>Gets the property that serves as a key to look up completion values against.</summary>
        protected abstract string KeyProperty { get; }

        protected static readonly ReadOnlyCollection<HtmlCompletion> Empty = new ReadOnlyCollection<HtmlCompletion>(new HtmlCompletion[0]);

        ///<summary>Creates a collection of HTML completion items from a list of static values.</summary>
        protected static ReadOnlyCollection<HtmlCompletion> Values(params string[] values)
        {
            return new ReadOnlyCollection<HtmlCompletion>(Array.ConvertAll(values, s => new SimpleHtmlCompletion(s)));
        }
        ///<summary>Creates a collection of HTML completion items from a list of static values.</summary>
        protected static ReadOnlyCollection<HtmlCompletion> Values(IEnumerable<string> values)
        {
            return new ReadOnlyCollection<HtmlCompletion>(values.Select(s => new SimpleHtmlCompletion(s)).ToList<HtmlCompletion>());
        }

        readonly IReadOnlyDictionary<string, IList<HtmlCompletion>> values;
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
