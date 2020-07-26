using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using csscript;

#if class_lib

namespace CSScriptLib
#else

namespace csscript
#endif
{
    /// <summary>
    ///
    /// </summary>
    static class CoreExtensions
    {
        /// <summary>
        /// Selects the first element that satisfies the specified path.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static XElement SelectFirst(this XContainer element, string path)
        {
            string[] parts = path.Split('/');

            var e = element.Elements()
                           .Where(el => el.Name.LocalName == parts[0])
                           .GetEnumerator();

            if (!e.MoveNext())
                return null;

            if (parts.Length == 1) //the last link in the chain
                return e.Current;
            else
                return e.Current.SelectFirst(path.Substring(parts[0].Length + 1)); //be careful RECURSION
        }

        /// <summary>
        /// Removes the duplicated file system path items from the collection.The duplicates are identified
        /// based on the path being case sensitive depending on the hosting OS file system.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public static string[] RemovePathDuplicates(this string[] list)
        {
            return list.Where(x => x.IsNotEmpty())
                       .Select(x =>
                       {
                           var fullPath = Path.GetFullPath(x);
                           if (File.Exists(fullPath))
                               return fullPath;
                           else
                               return x;
                       })
                       .Distinct()
                       .ToArray();
        }

        static string sdk_root = "".GetType().Assembly.Location.GetDirName();

        /// <summary>
        /// Determines whether [is shared assembly].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if [is shared assembly] [the specified path]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSharedAssembly(this string path) => path.StartsWith(sdk_root, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Converts to bool.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static bool ToBool(this string text) => text.ToLower() == "true";

        /// <summary>
        /// Removes the assembly extension.
        /// </summary>
        /// <param name="asmName">Name of the asm.</param>
        /// <returns></returns>
        public static string RemoveAssemblyExtension(this string asmName)
        {
            if (asmName.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) || asmName.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                return asmName.Substring(0, asmName.Length - 4);
            else
                return asmName;
        }

        /// <summary>
        /// Compares two path strings. Handles path being case-sensitive based on the OS file system.
        /// </summary>
        /// <param name="path1">The path1.</param>
        /// <param name="path2">The path2.</param>
        /// <returns></returns>
        public static bool SamePathAs(this string path1, string path2) =>
            string.Compare(path1, path2, Runtime.IsWin) == 0;

        /// <summary>
        /// Captures the exception dispatch information.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns></returns>
        public static Exception CaptureExceptionDispatchInfo(this Exception ex)
        {
            try
            {
                // on .NET 4.5 ExceptionDispatchInfo can be used
                // ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                typeof(Exception).GetMethod("PrepForRemoting", BindingFlags.NonPublic | BindingFlags.Instance)
                                 .Invoke(ex, new object[0]);
            }
            catch { /* non critical exception */ }
            return ex;
        }

        /// <summary>
        /// Files the delete.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="rethrow">if set to <c>true</c> [rethrow].</param>
        public static void FileDelete(this string filePath, bool rethrow)
        {
            //There are the reports about
            //anti viruses preventing file deletion
            //See 18 Feb message in this thread https://groups.google.com/forum/#!topic/cs-script/5Tn32RXBmRE

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    break;
                }
                catch
                {
                    if (rethrow && i == 2)
                        throw;
                }

                Thread.Sleep(300);
            }
        }

#if !class_lib

        public static List<string> AddIfNotThere(this List<string> items, string item, string section)
        {
            if (item != null && item != "")
            {
                bool isThere = items.Any(x => x.SamePathAs(item));

                if (!isThere)
                {
                    if (Settings.ProbingLegacyOrder)
                        items.Add(item);
                    else
                    {
                        var insideOfSection = false;
                        bool added = false;
                        for (int i = 0; i < items.Count; i++)
                        {
                            var currItem = items[i];
                            if (currItem == section)
                            {
                                insideOfSection = true;
                            }
                            else
                            {
                                if (insideOfSection && currItem.StartsWith(Settings.dirs_section_prefix))
                                {
                                    items.Insert(i, item);
                                    added = true;
                                    break;
                                }
                            }
                        }

                        // it's not critical at this stage as the whole options.SearchDirs (the reason for this routine)
                        // is rebuild from ground to top if it has no sections
                        var createMissingSection = false;

                        if (!added)
                        {
                            // just to the end
                            if (!insideOfSection && createMissingSection)
                                items.Add(section);

                            items.Add(item);
                        }
                    }
                }
            }
            return items;
        }

#endif

        public static string Escape(this char c)
        {
            return "\\u" + ((int)c).ToString("x4");
        }

        internal static string Expand(this string text) => Environment.ExpandEnvironmentVariables(text);

        internal static string UnescapeExpandTrim(this string text) =>
            CSharpParser.UnescapeDirectiveDelimiters(Environment.ExpandEnvironmentVariables(text)).Trim();

        internal static string NormaliseAsDirectiveOf(this string statement, string parentScript)
        {
            var text = CSharpParser.UserToInternalEscaping(statement);

            if (text.Length > 1 && (text[0] == '.' && text[1] != '.')) // just a single-dot start dir
                text = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(parentScript), text));

            return Environment.ExpandEnvironmentVariables(text).Trim();
        }

        internal static string NormaliseAsDirective(this string statement)
        {
            var text = CSharpParser.UnescapeDirectiveDelimiters(statement);
            return Environment.ExpandEnvironmentVariables(text).Trim();
        }

        internal static T2 Match<T1, T2>(this T1 value, params (T1, T2)[] patterenMap) where T1 : class
        {
            foreach (var (pattern, result) in patterenMap)
                if (value.Equals(pattern))
                    return result;

            return default(T2);
        }
    }
}