using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public static class CssExtensions
    {
        ///<summary>Gets the selector portion of the text of a Selector object, excluding any trailing comma.</summary>
        public static string SelectorText(this Selector selector)
        {
            if (selector.Comma == null) return selector.Text;
            return selector.Text.Substring(0, selector.Comma.Start - selector.Start).Trim();
        }
    }
}
