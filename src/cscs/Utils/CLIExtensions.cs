using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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

    // [Obsolete]
    // public static string[] Split(this string str, params string[] separators) =>
    //     str.Split(separators, str.Length, StringSplitOptions.None);

    // [Obsolete]
    // public static string[] Split(this string str, string[] separators, int count) =>
    //     str.Split(separators, count, StringSplitOptions.None);

    public static string[] GetLines(this string str) =>// too simplistic though adequate
        str.Replace("\r\n", "\n").Split('\n');

    public static string NormalizeNewLines(this string str) =>// too simplistic though adequate
        str.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

    public static IEnumerable<XElement> FindDescendants(this XElement element, string localName) =>
        element.Descendants().Where(x => x.Name.LocalName == localName);

    public static bool Contains(this string text, string pattern, bool ignoreCase) =>
        text.IndexOf(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : default(StringComparison)) != -1;

    public static bool StartsWith(this string text, string pattern, bool ignoreCase) =>
        text.StartsWith(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : default(StringComparison));

    // [Obsolete]
    // public static string[] ArgValues(this string[] arguments, string prefix) =>
    //     arguments.Where(x => x.StartsWith(prefix + ":"))
    //              .Select(x => x.Substring(prefix.Length + 1).TrimMatchingQuotes('"'))
    //              .ToArray();

    public static string ArgValue(this string[] arguments, string prefix) =>
        (arguments.FirstOrDefault(x => x.StartsWith(prefix + ":"))?.Substring(prefix.Length + 1).TrimMatchingQuotes('"'))
        ?? arguments.Where(x => x == prefix).Select(x => "").FirstOrDefault();

    // [Obsolete]
    // public static string ArgValue(this string argument, string prefix) =>
    //     argument.StartsWith(prefix + ":") == false ? null : argument.Substring(prefix.Length + 1)
    //             .TrimMatchingQuotes('"');

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

    // [Obsolete]
    // public static string[] SplitMergedArgs(this string[] args)
    // {
    //     //because Linux shebang does not properly split arguments we need to take care of this
    //     //http://www.daniweb.com/software-development/c/threads/268382

    //     // !!! the solution is not reliable
    //     // All #! args come as a single args item:
    //     // #! /home/user/tmp/app -1 -2 -3 -4
    //     // cmd: script.sh ./script -z
    //     // argv[0]-> /home/user/tmp/app
    //     // argv[1]-> -1 -2 -3 -4
    //     // argv[2]-> ./script
    //     // argv[3]-> -z

    //     // The real problem is that we do not know if an item needs splitting or it has been already done
    //     // script.sh "-1 -2 -3 -4"
    //     // argv[0]-> /home/user/tmp/app
    //     // argv[1]-> -1 -2 -3 -4
    //     // argv[2]-> -1 -2 -3 -4

    //     // `argv[1]` should be split and `argv[2]` should not but the decision about it cannot be made based on
    //     // the args content

    //     var result = args.SelectMany(SplitCommandLine)
    //                      .ToArray();
    //     return result;
    // }
}