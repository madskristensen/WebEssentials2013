using System.Collections.Generic;
using Microsoft.CSS.Core;
using Microsoft.JSON.Core.Parser;

namespace MadsKristensen.EditorExtensions.JSON
{
    internal class JSONItemCollector<T> : IJSONSimpleTreeVisitor where T : JSONParseItem
    {
        public IList<T> Items { get; private set; }
        private bool _includeChildren;

        public JSONItemCollector() : this(false) { }

        public JSONItemCollector(bool includeChildren)
        {
            _includeChildren = includeChildren;
            Items = new List<T>();
        }

        public VisitItemResult Visit(JSONParseItem parseItem)
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
