using System.Linq;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    internal class SelectorErrorTag : ICssError
    {
        private SortedRangeList<Selector> _range;
        private string _errorMessage;

        public SelectorErrorTag(SortedRangeList<Selector> range, string errorMessage)
        {
            _range = range;
            _errorMessage = errorMessage;
            Flags = WESettings.Instance.Css.ValidationLocation.ToCssErrorFlags();
        }


        public ParseItem Item
        {
            get { return _range.First(); }
        }

        public string Text { get; private set; }

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
