using System.Diagnostics;
using System.IO;
using System.Linq;
using System;

class Script
{
    static public void Main()
    {
        var src_dir = Path.GetFullPath(@"..\..\..\..\src\out\.NET Core");

        // var version = "1.2.0.0";
        var version = FileVersionInfo.GetVersionInfo(Path.Combine(src_dir, "cscs.dll")).FileVersion.ToString();

        var dest_dir = $"cs-script.core_{version}";

        void copy(string file, string new_file_name = null)
        {
            var s = Path.Combine(src_dir, file);
            var d = Path.Combine(dest_dir, new_file_name ?? file);
            Console.WriteLine("Copying:\n\t" + s + "\n\t" + d);
            File.Copy(s, d, true);
        }

        void generate_from_template(string file, Func<string, string> generator)
        {
            File.WriteAllText(file, generator(File.ReadAllText(file + ".template")));
            Process.Start(editor, file);
        }

        void replace_in_file(string file, Func<string, string> generator)
        {
            File.WriteAllText(file, generator(File.ReadAllText(file)));
        }

        Directory.CreateDirectory(dest_dir);
        foreach (var file in Directory.GetFiles(src_dir))
        {
            if (!file.EndsWith("css") && !file.EndsWith("css.exe"))
                copy(Path.GetFileName(file));
        }

        copy(@"..\-update.deb.cs", "-update.cs");
        copy(@"..\css.deb.sh", "css");

        File.WriteAllText(@"..\version.txt", version);

        // Mon, 14 Aug 2017 16:16:27 +1000
        var timestamp = $"{DateTime.Now:ddd, d MMM yyyy hh:mm:ss} " + $"{DateTime.Now:zzz} ".Replace(":", "");

        generate_from_template(@"debian\changelog",
                               text => text.Replace("${version}", version)
                                           .Replace("${date}", timestamp));

        generate_from_template(@"debian\install",
                               text => text.Replace("${version}", version));

        replace_in_file($@"cs-script.core_{version}\css",
                        text => text.Replace("${version}", version));

        Process.Start(editor, "readme.md");
        Process.Start("7z.exe", @"a -r -tzip .\..\build.zip .\*");

        Console.WriteLine("=====================");
        Console.WriteLine("Prepared for building package cs-script_" + version);
        Console.WriteLine("The build folder '.\\..\\build.zip' is ready.");
    }

    static string editor
    {
        get
        {
            var candidate = @"E:\Program Files\Sublime Text 3\sublime_text.exe";

            if (File.Exists(candidate))
                return candidate;

            candidate = @"c:\Program Files\Sublime Text 3\sublime_text.exe";
            if (File.Exists(candidate))
                return candidate;

            return "<unknown>";
        }
    }
}