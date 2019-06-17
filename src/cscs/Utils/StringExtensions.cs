using System;
using System.Collections.Generic;
using System.Linq;

#if class_lib

namespace CSScriptLib
#else
namespace csscript
#endif
{
    /// <summary>
    /// Various string extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Determines whether the string is empty (or null).
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if the specified text is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);

        /// <summary>
        /// Determines whether the string is not empty (or null).
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if [is not empty] [the specified text]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNotEmpty(this string text) => !string.IsNullOrEmpty(text);

        /// <summary>
        /// Trims a single character form the head and the end of the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="trimChars">The trim chars.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Compares two strings.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns></returns>
        public static bool SameAs(this string text, string pattern, bool ignoreCase = true)
            => 0 == string.Compare(text, pattern, ignoreCase);

        /// <summary>
        /// Joins strings the by the specified separator.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="separator">The separator.</param>
        /// <returns></returns>
        public static string JoinBy(this IEnumerable<string> values, string separator)
            => string.Join(separator, values);
    }
}