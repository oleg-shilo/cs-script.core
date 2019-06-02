using System;
using System.Collections.Generic;
using System.Linq;

namespace csscript
{
    public static class StringExtensions
    {
        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);

        public static bool IsNotEmpty(this string text) => !string.IsNullOrEmpty(text);

        public static string TrimSingle(this string text, params char[] trimChars)
        {
            if (text.IsEmpty())
                return text;

            var startOffset = trimChars.Contains(text[0]) ? 1 : 0;
            var endOffset = (trimChars.Contains(text.Last()) ? 1 : 0);

            if (startOffset != 0 || endOffset != 0)
                return text.Substring(startOffset, (text.Length - startOffset) - endOffset);
            else
                return text;
        }

        public static string JoinBy(this IEnumerable<string> values, string separator)
            => string.Join(separator, values);
    }
}