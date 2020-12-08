using Scripting;
using System;
using System.Text;

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
    static class CoreExtension
    {
        public static bool Contains(this string text, string value, StringComparison comparisonType)
            => text.IndexOf(value, comparisonType) != -1;

        public static string Replace(this string text, string oldValue, string newValue, StringComparison comparisonType)
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