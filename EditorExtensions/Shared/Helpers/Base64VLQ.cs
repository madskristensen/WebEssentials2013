using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    public class DecodedMap
    {
        public int GeneratedColumn { get; set; }
        public int GeneratedLine { get; set; }
        public int OriginalColumn { get; set; }
        public int OriginalLine { get; set; }
    }

    ///<summary>Variable Length Quantity (VLQ) Base 64 Serializer</summary>
    ///<remarks>Inspired by <see cref="https://github.com/mozilla/source-map"/></remarks>
    public static class Base64Vlq
    {
        // A single base 64 digit can contain 6 bits of data. For the base 64 variable
        // length quantities we use in the source map spec, the first bit is the sign,
        // the next four bits are the actual value, and the 6th bit is the
        // continuation bit. The continuation bit tells us whether there are more
        // digits in this value following this digit.
        //
        //   Continuation
        //   |    Sign
        //   |    |
        //   V    V
        //   101011

        const int VLQ_BASE_SHIFT = 5;

        // binary: 100000
        const int VLQ_BASE = 1 << VLQ_BASE_SHIFT;

        // binary: 011111
        const int VLQ_BASE_MASK = VLQ_BASE - 1;

        // binary: 100000
        const int VLQ_CONTINUATION_BIT = VLQ_BASE;

        private static Regex mappingSeparator = new Regex("^[,;]", RegexOptions.Compiled);

        /**
         * Converts from a two-complement value to a value where the sign bit is
         * is placed in the least significant bit.  For example, as decimals:
         *   1 becomes 2 (10 binary), -1 becomes 3 (11 binary)
         *   2 becomes 4 (100 binary), -2 becomes 5 (101 binary)
         */
        private static int ToVLQSigned(int value)
        {
            return value < 0 ? ((-value) << 1) + 1 : (value << 1) + 0;
        }

        /**
         * Converts to a two-complement value from a value where the sign bit is
         * is placed in the least significant bit.  For example, as decimals:
         *   2 (10 binary) becomes 1, 3 (11 binary) becomes -1
         *   4 (100 binary) becomes 2, 5 (101 binary) becomes -2
         */
        private static int FromVLQSigned(int value)
        {
            var isNegative = (value & 1) == 1;
            var shifted = value >> 1;

            return isNegative ? -shifted : shifted;
        }

        /**
         * Returns the base 64 VLQ encoded value.
         */
        public static string Encode(int number)
        {
            var encoded = "";
            int digit;

            var vlq = ToVLQSigned(number);

            do
            {
                digit = vlq & VLQ_BASE_MASK;

                vlq = vlq >> VLQ_BASE_SHIFT;

                if (vlq > 0)
                {
                    // There are still more digits in this value, so we must make sure the
                    // continuation bit is marked.
                    digit |= VLQ_CONTINUATION_BIT;
                }

                encoded += Base64.Base64Encode(digit);
            } while (vlq > 0);

            return encoded;
        }

        public static IEnumerable<DecodedMap> Decode(string vlqValue)
        {
            int generatedLine = 1, previousGeneratedColumn, previousOriginalLine, previousOriginalColumn;
            previousGeneratedColumn = previousOriginalLine = previousOriginalColumn = 0;

            while (vlqValue.Length > 0)
            {
                if (vlqValue[0] == ',')
                {
                    vlqValue = vlqValue.Substring(1);
                    continue;
                }

                if (vlqValue[0] == ';')
                {
                    generatedLine++;
                    vlqValue = vlqValue.Substring(1);
                    previousGeneratedColumn = 0;
                    continue;
                }

                var temp = Base64VLQDecode(vlqValue);
                var result = new DecodedMap();

                // Generated column.
                result.GeneratedColumn = previousGeneratedColumn + temp.value;
                result.GeneratedLine = generatedLine;
                previousGeneratedColumn = result.GeneratedColumn;

                vlqValue = temp.rest;

                if (vlqValue.Length < 1 || mappingSeparator.IsMatch(vlqValue.Substring(0, 1)))
                    yield break;

                // Original source.
                temp = Base64VLQDecode(vlqValue);
                vlqValue = temp.rest;

                if (vlqValue.Length == 0 || mappingSeparator.IsMatch(vlqValue.Substring(0, 1)))
                    throw new VlqException("Found a source, but no line and column");

                // Original line.
                temp = Base64VLQDecode(vlqValue);
                result.OriginalLine = previousOriginalLine + temp.value;
                previousOriginalLine = result.OriginalLine;
                // Lines are stored 0-based
                result.OriginalLine += 1;
                vlqValue = temp.rest;

                if (vlqValue.Length == 0 || mappingSeparator.IsMatch(vlqValue.Substring(0, 1)))
                    throw new VlqException("Found a source and line, but no column");

                // Original column.
                temp = Base64VLQDecode(vlqValue);
                result.OriginalColumn = previousOriginalColumn + temp.value;
                previousOriginalColumn = result.OriginalColumn;
                vlqValue = temp.rest;

                if (vlqValue.Length > 0 && !mappingSeparator.IsMatch(vlqValue.Substring(0, 1)))
                    // Skip Original Name bit; we are not using it.
                    vlqValue = vlqValue.Substring(1);

                yield return result;
            }
        }

        /**
         * Decodes the next base 64 VLQ value from the given string and returns the
         * value and the rest of the string.
         */
        public static dynamic Base64VLQDecode(string aStr)
        {
            var i = 0;
            var strLen = aStr.Length;
            var result = 0;
            var shift = 0;
            bool continuation;
            int digit;

            do
            {
                if (i >= strLen)
                    throw new VlqException("Expected more digits in base 64 VLQ value.");

                digit = Base64.Base64Decode(aStr[i++]);
                continuation = (digit & VLQ_CONTINUATION_BIT) != 0;
                digit &= VLQ_BASE_MASK;
                result = result + (digit << shift);
                shift += VLQ_BASE_SHIFT;
            } while (continuation);

            return new
            {
                value = FromVLQSigned(result),
                rest = aStr.Substring(i)
            };
        }

        private static class Base64
        {
            private static char[] _array = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".ToCharArray();

            /**
             * Encode an integer in the range of 0 to 63 to a single base 64 digit.
             */
            public static char Base64Encode(int number)
            {
                if (_array.Length > number)
                    return _array[number];

                throw new VlqException("Must be between 0 and 63: " + number);
            }

            /**
             * Decode a single base 64 digit to an integer.
             */
            public static int Base64Decode(char character)
            {
                var index = Array.IndexOf(_array, character);

                if (index > -1)
                    return index;

                throw new VlqException("Not a valid base 64 digit: " + character);
            }
        }
    }

    // For CA2201: Do not raise reserved exception types.
    [Serializable]
    public class VlqException : Exception
    {
        public VlqException()
            : base() { }

        public VlqException(string message)
            : base(message) { }

        public VlqException(string message, Exception innerException)
            : base(message, innerException) { }

        protected VlqException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}