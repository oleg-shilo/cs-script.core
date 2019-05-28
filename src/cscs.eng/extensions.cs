#region Licence...

//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2004-2018 Oleg Shilo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------

#endregion Licence...

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using csscript;

/// <summary>
/// Credit to https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
/// </summary>
public static class CLIExtensions
{
    public static string TrimMatchingQuotes(this string input, char quote)
    {
        if (input.Length >= 2)
        {
            //"-sconfig:My Script.cs.config"
            if (input.First() == quote && input.Last() == quote)
            {
                return input.Substring(1, input.Length - 2);
            }
            //-sconfig:"My Script.cs.config"
            else if (input.Last() == quote)
            {
                var firstQuote = input.IndexOf(quote);
                if (firstQuote != input.Length - 1) //not the last one
                    return input.Substring(0, firstQuote) + input.Substring(firstQuote + 1, input.Length - 2 - firstQuote);
            }
        }
        return input;
    }

    public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
    {
        int nextPiece = 0;

        for (int c = 0; c < str.Length; c++)
        {
            if (controller(str[c]))
            {
                yield return str.Substring(nextPiece, c - nextPiece);
                nextPiece = c + 1;
            }
        }

        yield return str.Substring(nextPiece);
    }

    public static string[] Split(this string str, params string[] separators)
    {
        return str.Split(separators, str.Length, StringSplitOptions.None);
    }

    public static string[] Split(this string str, string[] separators, int count)
    {
        return str.Split(separators, count, StringSplitOptions.None);
    }

    public static string[] GetLines(this string str) // too simplistic though adequate
    {
        return str.Replace("\r\n", "\n").Split('\n');
    }

    public static IEnumerable<XElement> FindDescendants(this XElement element, string localName)
    {
        return element.Descendants().Where(x => x.Name.LocalName == localName);
    }

    public static bool Contains(this string text, string pattern, bool ignoreCase)
    {
        return text.IndexOf(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : default(StringComparison)) != -1;
    }

    public static bool StartsWith(this string text, string pattern, bool ignoreCase)
    {
        return text.StartsWith(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : default(StringComparison));
    }

    public static string[] ArgValues(this string[] arguments, string prefix)
    {
        return arguments.Where(x => x.StartsWith(prefix + ":"))
                        .Select(x => x.Substring(prefix.Length + 1).TrimMatchingQuotes('"'))
                        .ToArray();
    }

    public static string ArgValue(this string[] arguments, string prefix)
    {
        return (arguments.FirstOrDefault(x => x.StartsWith(prefix + ":"))
                          ?.Substring(prefix.Length + 1).TrimMatchingQuotes('"'))

                ?? arguments.Where(x => x == prefix).Select(x => "").FirstOrDefault();
    }

    public static string ArgValue(this string argument, string prefix)
    {
        return argument.StartsWith(prefix + ":") == false ? null : argument.Substring(prefix.Length + 1).TrimMatchingQuotes('"');
    }

    public static string[] SplitCommandLine(this string commandLine)
    {
        bool inQuotes = false;
        bool isEscaping = false;

        return commandLine.Split(c =>
        {
            if (c == '\\' && !isEscaping) { isEscaping = true; return false; }

            if (c == '\"' && !isEscaping)
                inQuotes = !inQuotes;

            isEscaping = false;

            return !inQuotes && Char.IsWhiteSpace(c)/*c == ' '*/;
        })
        .Select(arg => arg.Trim().TrimMatchingQuotes('\"').Replace("\\\"", "\""))
        .Where(arg => !string.IsNullOrEmpty(arg))
        .ToArray();
    }
}

public static class SocketExtensions
{
    public static byte[] GetBytes(this string data)
    {
        return Encoding.UTF8.GetBytes(data);
    }

    public static string GetString(this byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    public static byte[] ReadAllBytes(this TcpClient client)
    {
        var bytes = new byte[client.ReceiveBufferSize];
        var len = client.GetStream()
                        .Read(bytes, 0, bytes.Length);
        var result = new byte[len];
        Array.Copy(bytes, result, len);
        return result;
    }

    public static string ReadAllText(this TcpClient client)
    {
        return client.ReadAllBytes().GetString();
    }

    public static void WriteAllBytes(this TcpClient client, byte[] data)
    {
        var stream = client.GetStream();
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }

    public static void WriteAllText(this TcpClient client, string data)
    {
        client.WriteAllBytes(data.GetBytes());
    }
}

class PotentiallyDeadCodeAttribute : Attribute
{
}

static class Serialization
{
    public static T Deserialize<T>(this string data)
    {
        var serializer = new XmlSerializer(typeof(T));

        using (var buffer = new StringReader(data))
        using (var reader = XmlReader.Create(buffer))
        {
            return (T)serializer.Deserialize(reader);
        }
    }

    public static string Serialize(this object obj)
    {
        if (obj == null) return "";

        var serializer = new XmlSerializer(obj.GetType());
        using (var string_writer = new StringWriter())
        using (var xml_writer = XmlWriter.Create(string_writer))
        {
            serializer.Serialize(xml_writer, obj);
            return string_writer.ToString();
        }
    }
}

public class BuildRequest
{
    public string Source;
    public string Assembly;
    public bool IsDebug;
    public string[] References;
}

public class BuildResult
{
    // using XML or JSON serializer is the best approach, though the first one  is heavy
    // and the second one requires an additional NuGet package, which even does not exist
    // for .NET Standard 2.0.
    // Thus for the temp solution like this go with a simple line serialization
    public class Diagnostic
    {
        /* Microsoft.CodeAnalysis.Diagnostic is coupled to the source tree and other CodeDOM objects
         * so need to use an adapter.
         */

        public static BuildResult.Diagnostic From(Microsoft.CodeAnalysis.Diagnostic data)
        {
            return new BuildResult.Diagnostic
            {
                IsWarningAsError = data.IsWarningAsError,
                Severity = data.Severity,
                Location_IsInSource = data.Location.IsInSource,
                Location_StartLinePosition_Line = data.Location.GetLineSpan().StartLinePosition.Line,
                Location_StartLinePosition_Character = data.Location.GetLineSpan().StartLinePosition.Character,
                Location_FilePath = data.Location.SourceTree.FilePath,
                Id = data.Id,
                Message = data.GetMessage()
            };
        }

        public bool IsWarningAsError;
        public DiagnosticSeverity Severity;
        public bool Location_IsInSource;
        public int Location_StartLinePosition_Line;
        public int Location_StartLinePosition_Character;
        public string Location_FilePath;
        public string Id;
        public string Message;
    }

    public bool Success;
    public List<BuildResult.Diagnostic> Diagnostics = new List<BuildResult.Diagnostic>();

    public static BuildResult From(EmitResult data)
    {
        return new BuildResult
        {
            Success = data.Success,
            Diagnostics = data.Diagnostics.Select(x => Diagnostic.From(x)).ToList()
        };
    }

    // public static BuildResult Deserialize(string data)
    // {
    //     var lines = data.GetLines();

    //     var refs = lines.ArgValues("-ref");
    //     var script = lines.ArgValue("-source");
    //     var isDbg = lines.ArgValue("-dbg") != null;
    //     var asmFile = lines.ArgValue("-asm");

    //     return new BuildResult
    //     {
    //         Success = lines.ArgValue("-success") == true.ToString()
    //         //,
    //         // Diagnostics = lines.ArgValues("-diagnostics").Select().ToList()
    //     };
    // }

    // public string Serialize()
    // {
    //     var request = new List<string>();
    //     request.Add("-success:" + this.Success);
    //     foreach (var item in this.Diagnostics)
    //         request.Add("-diagnostic:" + item.Serialize());

    //     return "";
    // }
}

public static class Runtime
{
    static public string NuGetCacheView => "<not defined>";

    public static bool IsWin { get; } = !isLinux;

    // Note it is not about OS being exactly Linux but rather about OS having Linux type of file system.
    // For example path being case sensitive
    public static bool isLinux { get; } = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX);

    public static bool IsMono { get; } = (Type.GetType("Mono.Runtime") != null);
    public static bool IsCore { get; } = "".GetType().Assembly.Location.Split(Path.DirectorySeparatorChar).Contains("Microsoft.NETCore.App");
    public static bool IsNet { get; } = !IsMono && !IsCore;
}

public static class CoreExtensions
{
    public static bool IsSamePathAs(this string path1, string path2)
    {
        return string.Compare(path1, path2, Runtime.IsWin) == 0;
    }

    /// <summary>
    /// A generic LINQ equivalent of C# foreach loop.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="action">The action.</param>
    /// <returns></returns>
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (T item in collection)
        {
            action(item);
        }
        return collection;
    }

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

    public static bool IsDynamic(this Assembly asm)
    {
        //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
        //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
        return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
    }

    public static string[] RemovePathDuplicates(this string[] list)
    {
        return list.Where(x => !string.IsNullOrEmpty(x))
                   .Select(x =>
                   {
                       var fullPath = Path.GetFullPath(x);
                       if (File.Exists(fullPath))
                           return fullPath;
                       else
                           return x;
                   })
                   .Distinct().ToArray();
    }

    public static string[] Distinct(this string[] list)
    {
        return Enumerable.Distinct(list).ToArray();
    }

    public static string[] ConcatWith(this string[] array1, IEnumerable<string> array2)
    {
        return array1.Concat(array2).ToArray();
    }

    public static string[] ConcatWith(this string[] array, string item)
    {
        return array.Concat(new[] { item }).ToArray();
    }

    public static string GetFileName(this string path) => Path.GetFileName(path);

    public static string GetFullPath(this string path) => Path.GetFullPath(path);

    public static string PathJoin(this string path, params object[] parts)
    {
        var allParts = new[] { path ?? "" }.Concat(parts.Select(x => x?.ToString() ?? ""));
        return Path.Combine(allParts.ToArray());
    }

    public static string[] ConcatWith(this string item, IEnumerable<string> array)
    {
        return new[] { item }.Concat(array).ToArray();
    }

    public static string GetDirName(this string path) => Path.GetDirectoryName(path);

    static string sdk_root = "".GetType().Assembly.Location.GetDirName();

    public static bool IsSharedAssembly(this string path) => path.StartsWith(sdk_root, StringComparison.OrdinalIgnoreCase);

    public static bool startsWith(this string text, string pattern) => text.StartsWith(pattern, StringComparison.Ordinal);

    public static int indexOf(this string text, string pattern) => text.IndexOf(pattern, StringComparison.Ordinal);

    public static bool ToBool(this string text) => text.ToLower() == "true";

    public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);

    public static bool IsNotEmpty(this string text) => !string.IsNullOrEmpty(text);

    public static string RemoveAssemblyExtension(this string asmName)
    {
        if (asmName.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) || asmName.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
            return asmName.Substring(0, asmName.Length - 4);
        else
            return asmName;
    }

    public static bool IsEmpty<T>(this IEnumerable<T> collection) => collection == null ? true : !collection.Any();

    public static string[] SplitMergedArgs(this string[] args)
    {
        //because Linux shebang does not properly split arguments we need to take care of this
        //http://www.daniweb.com/software-development/c/threads/268382

        var result = args.SelectMany(arg => arg.Split('-')
                                               .Select(x => x.Trim())
                                               .Where(x => x != "")
                                               .Select(x => "-" + arg))
                                               .ToArray();
        return result;
    }
}

namespace csscript
{
    public static class GenericExtensions
    {
        public static string PathGetDir(this string path)
            => path == null ? null : Path.GetDirectoryName(path);

#if !class_lib

        public static bool IsDirSectionSeparator(this string text)
        {
            return text != null && text.StartsWith(Settings.dirs_section_prefix) && text.StartsWith(Settings.dirs_section_suffix);
        }

#endif

        public static IEnumerable<TSource> AddItem<TSource>(this IEnumerable<TSource> items, TSource item)
        {
            return items.Concat(new[] { item });
        }

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

        /// <summary>
        /// Creates instance of a class from underlying assembly.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
        /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>
        /// Instance of the 'Type'. Throws an ApplicationException if the instance cannot be created.
        /// </returns>
        public static object CreateObject(this Assembly asm, string typeName, params object[] args)
        {
            return CreateInstance(asm, typeName, args);
        }

        /// <summary>
        /// Creates instance of a Type from underlying assembly.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <param name="typeName">Name of the type to be instantiated. Allows wild card character (e.g. *.MyClass can be used to instantiate MyNamespace.MyClass).</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>
        /// Created instance of the type.
        /// </returns>
        /// <exception cref="System.Exception">Type " + typeName + " cannot be found.</exception>
        private static object CreateInstance(Assembly asm, string typeName, params object[] args)
        {
            //note typeName for FindTypes does not include namespace
            if (typeName == "*")
            {
                //instantiate the first type found (but not auto-generated types)
                //Ignore Roslyn internal type: "Submission#N"; real script class will be Submission#0+Script
                foreach (Type type in asm.GetTypes())
                {
                    bool isMonoInternalType = (type.FullName == "<InteractiveExpressionClass>");
                    bool isRoslynInternalType = (type.FullName.StartsWith("Submission#") && !type.FullName.Contains("+"));

                    if (!isMonoInternalType && !isRoslynInternalType)
                    {
                        return Activator.CreateInstance(type, args);
                    }
                }
                return null;
            }
            else
            {
                var name = typeName.Replace("*.", "");

                Type[] types = asm.GetTypes()
                                  .Where(t => t.FullName.None(char.IsDigit)
                                              && (t.FullName == name
                                                    || t.FullName == ("Submission#0+" + name)
                                                    || t.Name == name))
                                      .ToArray();

                if (types.Length == 0)
                    throw new Exception("Type " + typeName + " cannot be found.");

                return Activator.CreateInstance(types.First(), args);
            }
        }

        internal static Type FirstUserTypeAssignableFrom<T>(this Assembly asm)
        {
            // exclude Roslyn internal types
            return asm
                .ExportedTypes
                .Where(t => t.FullName.None(char.IsDigit)           // 1 (yes Roslyn can generate class with this name)
                       && t.FullName.StartsWith("Submission#0+")  // Submission#0+Script
                          && !t.FullName.Contains("<<Initialize>>")) // Submission#0+<<Initialize>>d__0
                .FirstOrDefault(x => typeof(T).IsAssignableFrom(x));
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

        public static string Directory(this Assembly asm)
        {
            var file = asm.Location();
            if (file.IsNotEmpty())
                return Path.GetDirectoryName(file);
            else
                return "";
        }

        static bool None<T>(this IEnumerable<T> items, Func<T, bool> predicate) => !items.Any(predicate);

        //to avoid throwing the exception
        public static string Location(this Assembly asm)
        {
            if (asm.IsDynamic())
            {
                string location = Environment.GetEnvironmentVariable("location:" + asm.GetHashCode());
                if (location == null)
                    return "";
                else
                    return location ?? "";
            }
            else
                return asm.Location;
        }

        public static string GetName(this Type type)
        {
            return type.GetTypeInfo().Name;
        }

        public static List<T> AddIfNotThere<T>(this List<T> items, T item)
        {
            if (!items.Contains(item))
                items.Add(item);
            return items;
        }

        public static Exception CaptureExceptionDispatchInfo(this Exception ex)
        {
            try
            {
                // on .NET 4.5 ExceptionDispatchInfo can be used
                // ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                typeof(Exception).GetMethod("PrepForRemoting", BindingFlags.NonPublic | BindingFlags.Instance)
                                 .Invoke(ex, new object[0]);
            }
            catch { }
            return ex;
        }

#if !class_lib

        public static List<string> AddIfNotThere(this List<string> items, string item, string section)
        {
            if (item != null && item != "")
            {
                bool isThere = items.Any(x => x.IsSamePathAs(item));

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
    }

    /// <summary>
    /// Based on "maettu-this" proposal https://github.com/oleg-shilo/cs-script/issues/78
    /// His `SplitLexicallyWithoutTakingNewLineIntoAccount` is taken/used practically without any change.
    /// </summary>
    internal static class ConsoleStringExtensions
    {
        // for free form text when Console is not attached; so make it something big...
        public static int MaxNonConsoleTextWidth = 500;

        static int GetMaxTextWidth()
        {
            try
            {
                int width = 0;
                if (int.TryParse(Environment.GetEnvironmentVariable("Console.WindowWidth") ?? "", out int parentWidth))
                {
                    width = parentWidth - 1;
                }

                // when running on mono under Node.js (VSCode) the Console.WindowWidth is always 0
                if (width != 0)
                    return width - 1;
                else
                    return 100;
            }
            catch (Exception) { }
            return MaxNonConsoleTextWidth;
        }

        public static string Repeat(this char c, int count)
        {
            return new string(c, count);
        }

        public static string[] SplitSubParagraphs(this string text)
        {
            var lines = text.Split(new[] { "${<==}" }, 2, StringSplitOptions.None);
            if (lines.Count() == 2)
            {
                lines[1] = "${<=-" + lines[0].Length + "}" + lines[1];
            }
            return lines; ;
        }

        public static int Limit(this int value, int min, int max)
        {
            return value < min ? min :
                   value > max ? max :
                   value;
        }

        static string[] newLineSeparators = { Environment.NewLine, "\n", "\r" };
        // =============================================

        public static string ToConsoleLines(this string text, int indent)
        {
            return text.SplitIntoLines(GetMaxTextWidth(), indent);
        }

        public static string SplitIntoLines(this string text, int maxWidth, int indent)
        {
            var lines = new StringBuilder();
            string left_indent = ' '.Repeat(indent);

            foreach (string line in text.SplitLexically(maxWidth - indent))
                lines.Append(left_indent)
                     .AppendLine(line);

            return lines.ToString();
        }

        public static string[] SplitLexically(this string text, int desiredChunkLength)
        {
            var result = new List<string>();

            foreach (string item in text.Split(newLineSeparators, StringSplitOptions.None))
            {
                string paragraph = item;
                var extraIndent = 0;
                var merge = false;
                int firstDesiredChunkLength = desiredChunkLength;
                int allDesiredChunkLength = desiredChunkLength;

                string[] fittedLines = null;

                const string prefix = "${<=";

                if (paragraph.StartsWith(prefix)) // e.g. "${<=12}" or "${<=-12}" (to merge with previous line)
                {
                    var indent_info = paragraph.Split('}').First();

                    paragraph = paragraph.Substring(indent_info.Length + 1);
                    int.TryParse(indent_info.Substring(prefix.Length).Replace("}", ""), out extraIndent);

                    if (extraIndent != 0)
                    {
                        firstDesiredChunkLength =
                        allDesiredChunkLength = desiredChunkLength - Math.Abs(extraIndent);

                        if (extraIndent < 0)
                        {
                            merge = true;
                            if (result.Any())
                            {
                                firstDesiredChunkLength = desiredChunkLength - result.Last().Length;
                                firstDesiredChunkLength = firstDesiredChunkLength.Limit(0, desiredChunkLength);
                            }

                            if (firstDesiredChunkLength == 0)  // not enough space to merge so break it to the next line
                            {
                                merge = false;
                                firstDesiredChunkLength = allDesiredChunkLength;
                            }

                            extraIndent = -extraIndent;
                        }

                        fittedLines = SplitLexicallyWithoutTakingNewLineIntoAccount(paragraph, firstDesiredChunkLength, allDesiredChunkLength);

                        for (int i = 0; i < fittedLines.Length; i++)
                            if (i > 0 || !merge)
                                fittedLines[i] = ' '.Repeat(extraIndent) + fittedLines[i];
                    }
                }

                if (fittedLines == null)
                    fittedLines = SplitLexicallyWithoutTakingNewLineIntoAccount(paragraph, firstDesiredChunkLength, allDesiredChunkLength);

                if (merge && result.Any())
                {
                    result[result.Count - 1] = result[result.Count - 1] + fittedLines.FirstOrDefault();
                    fittedLines = fittedLines.Skip(1).ToArray();
                }

                result.AddRange(fittedLines);
            }
            return result.ToArray();
        }

        static string[] SplitLexicallyWithoutTakingNewLineIntoAccount(this string str, int desiredChunkLength)
        {
            return SplitLexicallyWithoutTakingNewLineIntoAccount(str, desiredChunkLength, desiredChunkLength);
        }

        static string[] SplitLexicallyWithoutTakingNewLineIntoAccount(this string str, int firstDesiredChunkLength, int desiredChunkLength)
        {
            var spaces = new List<int>();
            // Retrieve all spaces within the string:
            int i = 0;
            while ((i = str.IndexOf(' ', i)) >= 0)
            {
                spaces.Add(i);
                i++;
            }

            // Add an extra space at the end of the string to ensure that the last chunk is split properly:
            spaces.Add(str.Length);

            // Split the string into the desired chunk size taking word boundaries into account:
            int startIndex = 0;
            var chunks = new List<string>();
            int _desiredChunkLength = firstDesiredChunkLength;

            while (startIndex < str.Length)
            {
                if (startIndex != 0)
                    _desiredChunkLength = desiredChunkLength;

                // Find the furthermost split position:
                int spaceIndex = spaces.FindLastIndex(value => (value <= (startIndex + _desiredChunkLength)));

                int splitIndex;
                if (spaceIndex >= 0)
                    splitIndex = spaces[spaceIndex];
                else
                    splitIndex = (startIndex + _desiredChunkLength);

                // Limit to split within the string and execute the split:
                splitIndex = splitIndex.Limit(startIndex, str.Length);
                int length = (splitIndex - startIndex);
                chunks.Add(str.Substring(startIndex, length));
                Debug.WriteLine(chunks.Count());
                startIndex += length;

                // Remove the already used spaces from the collection to ensure those spaces are not used again:
                if (spaceIndex >= 0)
                {
                    spaces.RemoveRange(0, (spaceIndex + 1));
                    startIndex++; // Advance an extra character to compensate the space.
                }
            }

            return (chunks.ToArray());
        }

        // =============================================
    }

    internal static partial class CSSUtils
    {
        static public string[] GetDirectories(string workingDir, string rootDir)
        {
            if (!Path.IsPathRooted(rootDir))
                rootDir = Path.Combine(workingDir, rootDir); //cannot use Path.GetFullPath as it crashes if '*' or '?' are present

            List<string> result = new List<string>();

            if (rootDir.Contains("*") || rootDir.Contains("?"))
            {
                bool useAllSubDirs = rootDir.EndsWith("**");

                string pattern = WildCardToRegExpPattern(useAllSubDirs ? rootDir.Remove(rootDir.Length - 1) : rootDir);

                var wildcard = new Regex(pattern, Runtime.IsWin ? RegexOptions.IgnoreCase : RegexOptions.None);

                int pos = rootDir.IndexOfAny(new char[] { '*', '?' });

                string newRootDir = rootDir.Remove(pos);

                pos = newRootDir.LastIndexOf(Path.DirectorySeparatorChar);
                newRootDir = rootDir.Remove(pos);

                if (Directory.Exists(newRootDir))
                {
                    foreach (string dir in Directory.GetDirectories(newRootDir, "*", SearchOption.AllDirectories))
                        if (wildcard.IsMatch(dir))
                        {
                            if (!result.Contains(dir))
                            {
                                result.Add(dir);

                                if (useAllSubDirs)
                                    foreach (string subDir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
                                        //if (!result.Contains(subDir))
                                        result.Add(subDir);
                            }
                        }
                }
            }
            else
                result.Add(rootDir);

            return result.ToArray();
        }

        public static Regex WildCardToRegExp(this string pattern)
            => new Regex(pattern.WildCardToRegExpPattern(), Runtime.IsWin ? RegexOptions.IgnoreCase : RegexOptions.None);

        //Credit to MDbg team: https://github.com/SymbolSource/Microsoft.Samples.Debugging/blob/master/src/debugger/mdbg/mdbgCommands.cs
        public static string WildCardToRegExpPattern(this string simpleExp)
        {
            var sb = new StringBuilder();
            sb.Append("^");
            foreach (char c in simpleExp)
            {
                switch (c)
                {
                    case '\\':
                    case '{':
                    case '|':
                    case '+':
                    case '[':
                    case '(':
                    case ')':
                    case '^':
                    case '$':
                    case '.':
                    case '#':
                    case ' ':
                        sb.Append('\\').Append(c);
                        break;

                    case '*':
                        sb.Append(".*");
                        break;

                    case '?':
                        sb.Append(".");
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append("$");
            return sb.ToString();
        }
    }
}