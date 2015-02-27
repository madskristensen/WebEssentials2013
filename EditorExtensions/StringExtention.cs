using System;
using System.Linq;

namespace System
{
    public static class StringExtention
    {
        #region IndexOfAny

        public static int IndexOfAny(this string s, string[] anyOf)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.IndexOf(value);

                if (index > -1)
                    break;
            }
            return index;
        }

        public static int IndexOfAny(this string s, string[] anyOf, int startIndex)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.IndexOf(value, startIndex);

                if (index > -1)
                    break;
            }
            return index;
        }

        public static int IndexOfAny(this string s, string[] anyOf, int startIndex, StringComparison comparisonType)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.IndexOf(value, startIndex, comparisonType);

                if (index > -1)
                    break;
            }
            return index;
        }

        public static int IndexOfAny(this string s, string[] anyOf, int startIndex, int count)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.IndexOf(value, startIndex, count);

                if (index > -1)
                    break;
            }
            return index;
        }

        public static int IndexOfAny(this string s, string[] anyOf, int startIndex, int count, StringComparison comparisonType)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.IndexOf(value, startIndex, count, comparisonType);

                if (index > -1)
                    break;
            }
            return index;
        }

        #endregion

        #region LastIndexOfAny

        public static int LastIndexOfAny(this string s, string[] anyOf)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.LastIndexOf(value);

                if (index > -1)
                    break;
            }
            return index;
        }

        public static int LastIndexOf(this string s, string[] anyOf, int startIndex)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.LastIndexOf(value, startIndex);

                if (index > -1)
                    break;
            }
            return index;
        }

        public static int LastIndexOf(this string s, string[] anyOf, int startIndex, StringComparison comparisonType)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.LastIndexOf(value, startIndex, comparisonType);

                if (index > -1)
                    break;
            }
            return index;
        }

        public static int LastIndexOf(this string s, string[] anyOf, int startIndex, int count)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.LastIndexOf(value, startIndex, count);

                if (index > -1)
                    break;
            }
            return index;
        }

        public static int LastIndexOf(this string s, string[] anyOf, int startIndex, int count, StringComparison comparisonType)
        {
            var index = -1;

            foreach (var value in anyOf)
            {
                index = s.LastIndexOf(value, startIndex, count, comparisonType);

                if (index > -1)
                    break;
            }
            return index;
        }

        #endregion
    }
}