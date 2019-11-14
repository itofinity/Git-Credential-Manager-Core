using System;
using System.Linq;

namespace DotNetAuth.OAuth1a.Framework
{
    /// <summary>
    /// Methods to percent encode and decode strings.
    /// </summary>
    /// <remarks>
    /// This class aims to implement parameter encoding according to RFC3986 as it is required in OAuth1.0a.
    /// For more details go to <link address="http://tools.ietf.org/html/rfc3986">http://tools.ietf.org/html/rfc3986</link>
    /// </remarks>
    public class PercentEncode
    {
        /// <summary>
        /// Percent encodes the source string.
        /// </summary>
        /// <param name="source">The string to be encoded.</param>
        /// <returns>The percent encoded string.</returns>
        public static string Encode(string source)
        {
            Func<char, string> encodeCharacter = c => {
                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c == '.' || c == '-' || c == '_' || c == '~'))
                    return new string(c, 1);
                return EncodeCharacter(c);
            };
            return string.Concat(source.ToCharArray().Select(encodeCharacter).ToArray());
        }
        /// <summary>
        /// If the input value is encoded decodes the value.
        /// </summary>
        /// <param name="encodedValue">A string which is percent encoded.</param>
        /// <returns>The decoded string.</returns>
        public static string Decode(string encodedValue)
        {
            var indexList = encodedValue.ToCharArray().Select((c, index) => new { index, target = c == '%' }).Where(c => c.target).Select(c => c.index).ToArray();
            var value = encodedValue;
            var codesToReplace = indexList.Select(index => value.Substring(index + 1, 2)).Distinct();
            var replacements = codesToReplace.Select(code => new { code, replacement = DecodePercentValue(code) }).ToArray();
            foreach (var item in replacements) encodedValue = encodedValue.Replace("%" + item.code, item.replacement);
            return encodedValue;
        }
        private static string EncodeCharacter(char c)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(new[] { c });
            return string.Concat(bytes.Select(b => "%" + b.ToString("X2")).ToArray());
        }
        private static string DecodePercentValue(string code)
        {
            var value = int.Parse(code, System.Globalization.NumberStyles.HexNumber);
            var bigByte = (byte)(value / 256);
            var smallByte = (byte)(value % 256);
            var bytes = bigByte != 0 ? new[] { bigByte, smallByte } : new[] { smallByte };
            var replacement = System.Text.Encoding.UTF8.GetString(bytes);
            return replacement;
        }
    }
}
