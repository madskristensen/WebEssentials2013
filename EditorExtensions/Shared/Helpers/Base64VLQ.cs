using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
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

        private static bool IsMappingSeparator(int ch) { return ch == ',' || ch == ';'; }

        private static string GetName(int index, string basePath, params string[] sources)
        {
            if (sources.Length == 0)
                return string.Empty;

            if (sources.Length > index)
                return Path.GetFullPath(Path.Combine(basePath, sources[index]));

            throw new VlqException("Invalid index received.");
        }

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
            var encoded = new StringBuilder();
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

                encoded.Append(Base64.Base64Encode(digit));
            } while (vlq > 0);

            return encoded.ToString();
        }

        public static IEnumerable<CssSourceMapNode> Decode(string vlqValue, string basePath, params string[] sources)
        {
            int generatedLine = 0, previousSource, previousGeneratedColumn, previousOriginalLine, previousOriginalColumn;
            previousSource = previousGeneratedColumn = previousOriginalLine = previousOriginalColumn = 0;

            var stream = new StringReader(vlqValue);
            while (stream.Peek() != -1)
            {
                if (stream.Peek() == ',')
                {
                    stream.Read();
                    continue;
                }

                if (stream.Peek() == ';')
                {
                    stream.Read();
                    generatedLine++;
                    previousGeneratedColumn = 0;
                    continue;
                }

                var result = new CssSourceMapNode();

                // Generated column.
                result.GeneratedColumn = previousGeneratedColumn + VlqDecode(stream);
                result.GeneratedLine = generatedLine;
                previousGeneratedColumn = result.GeneratedColumn;

                if (stream.Peek() < 0 || IsMappingSeparator(stream.Peek()))
                    yield break;

                // Original source.
                previousSource += VlqDecode(stream);
                result.SourceFilePath = GetName(previousSource, basePath, sources);

                if (stream.Peek() < 0 || IsMappingSeparator(stream.Peek()))
                    throw new VlqException("Found a source, but no line and column");

                // Original line.
                result.OriginalLine = previousOriginalLine + VlqDecode(stream);
                previousOriginalLine = result.OriginalLine;

                if (stream.Peek() < 0 || IsMappingSeparator(stream.Peek()))
                    throw new VlqException("Found a source and line, but no column");

                // Original column.
                result.OriginalColumn = previousOriginalColumn + VlqDecode(stream);
                previousOriginalColumn = result.OriginalColumn;

                // Skip Original Name bit; we are not using it.
                if (stream.Peek() > 0 && !IsMappingSeparator(stream.Peek()))
                    stream.Read();

                yield return result;
            }
        }

        ///<summary>Reads a single VLQ value from a stream of text, advancing the stream to the subsequent character.</summary>

        public static int VlqDecode(TextReader stream)
        {
            var result = 0;
            var shift = 0;
            bool continuation;
            int digit;

            do
            {
                if (stream.Peek() == -1)
                    throw new VlqException("Expected more digits in base 64 VLQ value.");

                digit = Base64.Base64Decode((char)stream.Read());
                continuation = (digit & VLQ_CONTINUATION_BIT) != 0;
                digit &= VLQ_BASE_MASK;
                result += digit << shift;
                shift += VLQ_BASE_SHIFT;
            } while (continuation);

            return FromVLQSigned(result);
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
            : base()
        { }

        public VlqException(string message)
            : base(message)
        { }

        public VlqException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected VlqException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}