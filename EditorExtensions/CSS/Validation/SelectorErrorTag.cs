using System.Linq;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class SelectorErrorTag : ICssError
    {
        private SortedRangeList<Selector> _range;

        public SelectorErrorTag(SortedRangeList<Selector> range, string text)
        {
            _range = range;
            Flags = WESettings.Instance.Css.ValidationLocation.ToCssErrorFlags();
            Text = text;
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
