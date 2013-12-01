using System;

namespace MadsKristensen.EditorExtensions
{
    public static class StringExtensions
    {
        internal static bool ValidateNumericality(this string input)
        {
            foreach (char digit in input)
            {
                if (!Char.IsDigit(digit) && digit != '.')
                {
                    return false;
                }
            }
            return true;
        }
    }
}
