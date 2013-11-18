using System.Collections.Generic;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Creates a list of CSS ParseItems of a certain type
    /// (when passed in as the visitor to any item's Accept() function)
    /// </summary>
    internal class CssItemCollector<T> : ICssSimpleTreeVisitor where T : ParseItem
    {
        public IList<T> Items { get; private set; }
        private bool _includeChildren;

        public CssItemCollector() : this(false) { }

        public CssItemCollector(bool includeChildren)
        {
            _includeChildren = includeChildren;
            Items = new List<T>();
        }

        public VisitItemResult Visit(ParseItem parseItem)
        {
            var item = parseItem as T;

            if (item != null)
            {
                Items.Add(item);
                return (_includeChildren) ? VisitItemResult.Continue : VisitItemResult.SkipChildren;
            }

            return VisitItemResult.Continue;
        }
    }
}
