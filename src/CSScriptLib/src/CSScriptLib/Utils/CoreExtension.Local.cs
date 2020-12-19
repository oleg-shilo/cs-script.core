using System;
using System.Text;
using Scripting;

namespace Scripting
{
    class CSScriptException : ApplicationException
    {
        public CSScriptException(string message = null) : base(message)
        {
        }
    }
}

namespace CSScriptLib
{
    /// <summary>
    /// CSScriptLib is compiled as nets standard so some .NETCore API is not available.
    /// So filling the gaps...
    /// </summary>
    public static class CoreExtension
    {
        /// <summary>
        /// Escapes the CS-Script directive (e.g. //css_*) delimiters.
        /// <para>All //css_* directives should escape any internal CS-Script delimiters by doubling the delimiter character.
        /// For example //css_include for 'script(today).cs' should escape brackets as they are the directive delimiters.
        /// The correct syntax would be as follows '//css_include script((today)).cs;'</para>
        /// <remarks>The delimiters characters are ';,(){}'.
        /// <para>However you should check <see cref="csscript.CSharpParser.DirectiveDelimiters"/> for the accurate list of all delimiters.
        /// </para>
        /// </remarks>
        /// </summary>
        /// <param name="text">The text to be processed.</param>
        /// <returns></returns>
        public static string EscapeDirectiveDelimiters(this string text)
        {
            foreach (char c in CSharpParser.DirectiveDelimiters)
                text = text.Replace(c.ToString(), new string(c, 2)); //very unoptimized but it is intended only for troubleshooting.
            return text;
        }

        internal static bool Contains(this string text, string value, StringComparison comparisonType)
            => text.IndexOf(value, comparisonType) != -1;

        internal static string Replace(this string text, string oldValue, string newValue, StringComparison comparisonType)
        {
            var result = new StringBuilder();

            var pos = 0;
            var prevPos = 0;

            while ((pos = text.IndexOf(oldValue, pos, comparisonType)) != -1)
            {
                result.Append(text.Substring(prevPos, pos - prevPos));
                result.Append(newValue);
                prevPos = pos;
                pos += oldValue.Length;
            }
            result.Append(text.Substring(prevPos, text.Length - prevPos));

            return result.ToString();
        }
    }
}