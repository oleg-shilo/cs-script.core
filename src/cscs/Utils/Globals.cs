using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CSScripting
{
    public partial class Globals
    {
        static internal string DynamicWrapperClassName = "DynamicClass";
        static internal string RootClassName = "css_root";
        // Roslyn still does not support anything else but `Submission#0` (17 Jul 2019)
        // [update] Roslyn now does support alternative class names (1 Jan 2020)

        static internal string build_server = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("build.dll");

        static public string csc_dll
        {
            get
            {
                // linux ~dotnet/.../3.0.100-preview5-011568/Roslyn/... (cannot find in preview)
                // win: program_files/dotnet/sdk/<version>/Roslyn/csc.exe
                var dotnet_root = "".GetType().Assembly.Location;

                // find first "dotnet" parent dir by trimming till the last "dotnet" token
                dotnet_root = dotnet_root.Split(Path.DirectorySeparatorChar)
                                         .Reverse()
                                         .SkipWhile(x => x != "dotnet")
                                         .Reverse()
                                         .JoinBy(Path.DirectorySeparatorChar.ToString());

                if (dotnet_root.PathJoin("sdk").DirExists()) // need to check as otherwise it will throw
                {
                    var dirs = dotnet_root.PathJoin("sdk")
                                          .PathGetDirs("*")
                                          .Where(dir => char.IsDigit(dir.GetFileName()[0]))
                                          .OrderBy(x => System.Version.Parse(x.GetFileName().Split('-').First()))
                                          .SelectMany(dir => dir.PathGetDirs("Roslyn"))
                                          .ToArray();
                    var csc_exe = dirs.Select(dir => dir.PathJoin("bincore", "csc.dll"))
                                      .LastOrDefault(File.Exists);
                    return csc_exe;
                }
                return null;
            }
        }
    }
}