using System.Linq;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    internal class SelectorErrorTag : ICssError
    {
        private SortedRangeList<Selector> _range;

        public SelectorErrorTag(SortedRangeList<Selector> range)
        {
            _range = range;
            Flags = WESettings.Instance.Css.ValidationLocation.ToCssErrorFlags();
        }

        public ParseItem Item
        {
            get { return _range.First(); }
        }

        public string Text { get { return null; } } // TODO: Add text?

        public int AfterEnd
        {
            get { return _range.Last().AfterEnd; }
        }

        public int Length
        {
            get { return AfterEnd - Start; }
        }

        public int Start
        {
            get { return _range.First().Start; }
        }

        public CssErrorFlags Flags { get; private set; }
    }
}
