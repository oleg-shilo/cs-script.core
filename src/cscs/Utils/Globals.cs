using CSScriptLib;
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

        static internal string build_server
            => Environment.SpecialFolder.LocalApplicationData.GetPath().PathJoin("cs-script",
                                                                                 "bin",
                                                                                 "compiler",
                                                                                 Assembly.GetExecutingAssembly().GetName().Version,
                                                                                 "build.dll");

        static public bool BuildServerIsDeployed
        {
            get
            {
                if (!build_server.FileExists())
                {
                    Directory.CreateDirectory(build_server.GetDirName());
                    File.WriteAllBytes(build_server, Resources.build);
                    File.WriteAllBytes(build_server.ChangeExtension(".deps.json"), Resources.build_deps);
                    File.WriteAllBytes(build_server.ChangeExtension(".runtimeconfig.json"), Resources.build_runtimeconfig);
                }

                return build_server.FileExists();
            }
        }

        static string csc_file;

        static public string csc
        {
            set
            {
                csc_file = value;
            }

            get
            {
                if (csc_file == null)
                {
#if class_lib
                    if (!Runtime.IsCore)
                    {
                        csc_file = Path.Combine(Path.GetDirectoryName("".GetType().Assembly.Location), "csc.exe");
                    }
                    else
#endif
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
                            csc_file = dirs.Select(dir => dir.PathJoin("bincore", "csc.dll"))
                                                .LastOrDefault(File.Exists);
                        }
                    }
                }
                return csc_file;
            }
        }
    }
}