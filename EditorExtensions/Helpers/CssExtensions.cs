using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public static class CssExtensions
    {
        ///<summary>Gets the selector portion of the text of a Selector object, excluding any trailing comma.</summary>
        public static string SelectorText(this Selector s)
        {
            if (s.Comma == null) return s.Text;
            return s.Text.Substring(0, s.Comma.Start - s.Start).Trim();
        }
    }
}
