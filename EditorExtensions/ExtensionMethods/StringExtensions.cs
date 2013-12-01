using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    public static class StringExtensions
    {
        internal static bool ValidateNumericality(this string input)
        {
            return input.All(digit => char.IsDigit(digit) || digit.Equals('.'));
        }
    }
}
