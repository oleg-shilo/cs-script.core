using System.IO;
using System.Linq;

#if class_lib

namespace CSScriptLib
#else

namespace csscript
#endif
{
    /// <summary>
    /// Various PATH extensions
    /// </summary>
    internal static class PathExtensions
    {
        public static string GetExtension(this string path) => Path.GetExtension(path);

        public static string GetFileName(this string path) => Path.GetFileName(path);

        public static bool DirExists(this string path) => path.IsNotEmpty() ? Directory.Exists(path) : false;

        public static string GetFullPath(this string path) => Path.GetFullPath(path);

        public static string PathJoin(this string path, params object[] parts)
        {
            var allParts = new[] { path ?? "" }.Concat(parts.Select(x => x?.ToString() ?? ""));
            return Path.Combine(allParts.ToArray());
        }

        public static string GetDirName(this string path)
            => path == null ? null : Path.GetDirectoryName(path);

#if !class_lib

        public static bool IsDirSectionSeparator(this string text)
        {
            return text != null && text.StartsWith(Settings.dirs_section_prefix) && text.StartsWith(Settings.dirs_section_suffix);
        }

#endif
    }
}