using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Crawls a CSS parse tree, gathering node of various types into a uniform collection.
    /// This class allows you to gather multiple types of ParseItems without recrawling the
    /// tree for each type.
    /// To use this class, add a collection initializer with a list of typed lambdas.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Justification = "Not actually a collection; implements IEnumerable for initializer syntax")]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public sealed class CssItemAggregator<TResult> : ICssSimpleTreeVisitor, System.Collections.IEnumerable   // For collection initializer syntax
    {
        public ReadOnlyCollection<TResult> Items { get; private set; }
        private readonly bool _includeChildren;
        private readonly List<TResult> _writableItems = new List<TResult>();
        private readonly List<Func<ParseItem, bool>> _funcs = new List<Func<ParseItem, bool>>();
        private readonly Func<ParseItem, bool> _filter;

        public CssItemAggregator() : this(false) { }

        public CssItemAggregator(Func<ParseItem, bool> filter)
            : this(false)
        {
            _filter = filter;
        }

        public CssItemAggregator(bool includeChildren)
        {
            _includeChildren = includeChildren;
            Items = new ReadOnlyCollection<TResult>(_writableItems);
        }

        public void Add<TNode>(Func<TNode, TResult> selector) where TNode : ParseItem
        {
            _funcs.Add(item =>
            {
                var typedItem = item as TNode;
                if (typedItem == null)
                    return false;
                _writableItems.Add(selector(typedItem));
                return true;
            });
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "multi")]
        public void Add<TNode>(Func<TNode, IEnumerable<TResult>> multiSelector) where TNode : ParseItem
        {
            _funcs.Add(item =>
            {
                var typedItem = item as TNode;
                if (typedItem == null)
                    return false;
                _writableItems.AddRange(multiSelector(typedItem));
                return true;
            });
        }

        VisitItemResult ICssSimpleTreeVisitor.Visit(ParseItem parseItem)
        {
            if (_filter != null && !_filter(parseItem))
                return VisitItemResult.SkipChildren;

            foreach (var func in _funcs)
            {
                if (!func(parseItem))
                    continue;
                if (!_includeChildren)
                    return VisitItemResult.SkipChildren;
            }
            return VisitItemResult.Continue;
        }

        public ReadOnlyCollection<TResult> Crawl(ParseItem root)
        {
            root.Accept(this);
            return Items;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException("Not actually a collection");
        }
    }
}
