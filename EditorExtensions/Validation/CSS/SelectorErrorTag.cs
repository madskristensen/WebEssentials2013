using Microsoft.CSS.Core;
using System.Linq;

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
            Flags = GetLocation();
        }

        private static CssErrorFlags GetLocation() 
        {
            switch ((WESettings.Keys.ErrorLocation)WESettings.GetInt(WESettings.Keys.CssErrorLocation))
            {
                case WESettings.Keys.ErrorLocation.Warnings:
                    return CssErrorFlags.UnderlinePurple | CssErrorFlags.TaskListWarning;

                default:
                    return CssErrorFlags.UnderlinePurple | CssErrorFlags.TaskListMessage;
            }            
        }

        public bool IsExposedToUser
        {
            get { return true; }
        }

        public ParseItem Item
        {
            get { return _range.First(); }
        }

        public string Text
        {
            get { return _errorMessage; }
        }

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

        public CssErrorFlags Flags {get; set; }
    }
}
