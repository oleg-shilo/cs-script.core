using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CSScripting
{
    /// <summary>
    /// Various PATH extensions
    /// </summary>
    public static class PathExtensions
    {
        public static void FileCopy(this string src, string dest, bool ignoreErrors = false)
        {
            try
            {
                File.Copy(src, dest, true);
            }
            catch
            {
                if (!ignoreErrors) throw;
            }
        }

        public static string ChangeExtension(this string path, string extension) => Path.ChangeExtension(path, extension);

        public static string GetExtension(this string path) => Path.GetExtension(path);

        public static string GetFileName(this string path) => Path.GetFileName(path);

        public static bool DirExists(this string path) => path.IsNotEmpty() ? Directory.Exists(path) : false;

        public static string GetFullPath(this string path) => Path.GetFullPath(path);

        public static bool IsDir(this string path) => Directory.Exists(path);

        public static string PathJoin(this string path, params object[] parts)
        {
            var allParts = new[] { path ?? "" }.Concat(parts.Select(x => x?.ToString() ?? ""));
            return Path.Combine(allParts.ToArray());
        }

        public static string GetPath(this Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }

        public static string EnsureDir(this string path, bool rethrow = true)
        {
            try
            {
                Directory.CreateDirectory(path);

                return path;
            }
            catch { if (rethrow) throw; }
            return null;
        }

        public static string DeleteDir(this string path, bool handeExceptions = false)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    void del_dir(string d)
                    {
                        try { Directory.Delete(d); }
                        catch (Exception)
                        {
                            Thread.Sleep(1);
                            Directory.Delete(d);
                        }
                    }

                    var dirs = new Queue<string>();
                    dirs.Enqueue(path);

                    while (dirs.Any())
                    {
                        var dir = dirs.Dequeue();

                        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                            File.Delete(file);

                        Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)
                                 .ForEach(dirs.Enqueue);
                    }

                    var emptyDirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                                             .Reverse();

                    emptyDirs.ForEach(del_dir);

                    del_dir(path);
                }
                catch
                {
                    if (!handeExceptions) throw;
                }
            }
            return path;
        }

        public static bool FileExists(this string path) => path.IsNotEmpty() ? File.Exists(path) : false;

        public static string GetDirName(this string path)
            => path == null ? null : Path.GetDirectoryName(path);

        public static string ChangeFileName(this string path, string fileName) => path.GetDirName().PathJoin(fileName);

        public static string GetFileNameWithoutExtension(this string path) => Path.GetFileNameWithoutExtension(path);

        public static string PathNormaliseSeparators(this string path)
        {
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public static string[] PathGetDirs(this string path, string mask)
        {
            return Directory.GetDirectories(path, mask);
        }

#if !class_lib

        public static bool IsDirSectionSeparator(this string text)
        {
            return text != null && text.StartsWith(csscript.Settings.dirs_section_prefix) && text.StartsWith(csscript.Settings.dirs_section_suffix);
        }

#endif
    }
}