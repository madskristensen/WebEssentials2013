using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    internal class SimpleErrorTag : ICssError
    {

        public SimpleErrorTag(ParseItem item, string errorMessage, CssErrorFlags flags)
            : this(item, errorMessage, item.AfterEnd - item.Start, flags)
        { }

        public SimpleErrorTag(ParseItem item, string errorMessage)
            : this(item, errorMessage, item.AfterEnd - item.Start)
        { }

        public SimpleErrorTag(ParseItem item, string errorMessage, int length)
            : this(item, errorMessage, length, WESettings.Instance.Css.ValidationLocation.ToCssErrorFlags())
        { }
        public SimpleErrorTag(ParseItem item, string errorMessage, int length, CssErrorFlags flags)
        {
            Item = item;
            Text = errorMessage;
            Length = length;
            Flags = flags;
        }

        public ParseItem Item { get; private set; }
        public string Text { get; private set; }

        public int AfterEnd { get { return Item.AfterEnd; } }
        public int Start { get { return Item.Start; } }
        public int Length { get; private set; }
        public CssErrorFlags Flags { get; private set; }
    }
}
