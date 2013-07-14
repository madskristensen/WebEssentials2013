using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    internal class SimpleErrorTag : ICssError
    {
        private ParseItem _item;
        private string _errorMessage;
        private int _length;

        public SimpleErrorTag(ParseItem item, string errorMessage, CssErrorFlags flags = CssErrorFlags.TaskListMessage | CssErrorFlags.UnderlinePurple)
        {
            _item = item;
            _errorMessage = errorMessage;
            _length = AfterEnd - Start;
            Flags = flags;
        }

        public SimpleErrorTag(ParseItem item, string errorMessage)
        {
            _item = item;
            _errorMessage = errorMessage;
            _length = AfterEnd - Start;
            Flags = GetLocation();
        }

        public SimpleErrorTag(ParseItem item, string errorMessage, int length)
        {
            _item = item;
            _errorMessage = errorMessage;
            _length = length;
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
            get { return _item; }
        }

        public string Text
        {
            get { return _errorMessage; }
        }

        public int AfterEnd
        {
            get { return _item.AfterEnd; }
        }

        public int Length
        {
            get { return _length; }
        }

        public int Start
        {
            get { return _item.Start; }
        }

        public CssErrorFlags Flags {get; set; }
    }
}
