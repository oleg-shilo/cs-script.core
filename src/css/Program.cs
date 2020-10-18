using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Linq;
using System.Reflection;

/// <summary>
/// .NET Full/Mono app launcher for CS-Script .NET Core host. This executable is a simple process router that forks
/// a .NET Core host for CS-Script engine class library assembly. It simply executes "dotnet cscs.dll script.cs" and redirects
/// STD input, error and output.
/// It's just a convenient way to launch CS-Script .NET Core with minimal typing.
/// Runtime: .NET Framework 4.6.1
/// File name: cssc.exe
/// Example: "cssc.exe script.cs"
/// </summary>
namespace css
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Environment.SetEnvironmentVariable("CSS_WINAPP", "true", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("Console.WindowWidth", Console.WindowWidth.ToString());
            Environment.SetEnvironmentVariable("ENTRY_ASM", Assembly.GetExecutingAssembly().GetName().Name);
            bool hideConsole = false;

            if (args.Contains("-noconsole") || args.Contains("-nc"))
            {
                hideConsole = true;
                args = args.Where(a => a != "-noconsole" && a != "-nc").ToArray();
            }

            if (args.ParseValuedArg("runtime", "rt", out string value))
            {
                var full_env = Environment.OSVersion.IsWin() ? ".NET" : "Mono";

                args = args.Where(a => !(a.StartsWith("-rt") || a.StartsWith("-runtime"))).ToArray();
                if (value == null)
                {
                    if (args.Contains("?") || args.Contains("help"))
                    {
                        Console.WriteLine("-rt|-runtime[:<core|full>]\n" +
                                          "    Sets the execution engine to .NET Core or the full version of .NET/Mono.");
                    }
                    else
                    {
                        if (File.Exists(RedirectFileName))
                            Console.WriteLine($"The execution engine is set to {full_env}");
                        else
                            Console.WriteLine($"The execution engine is set to .NET Core");
                    }
                }
                else
                {
                    switch (value.ToLower())
                    {
                        case "full":
                            File.WriteAllText(RedirectFileName, "");
                            Console.WriteLine($"The execution engine is set to {full_env}");
                            break;

                        case "core":
                            if (File.Exists(RedirectFileName))
                                File.Delete(RedirectFileName);
                            Console.WriteLine($"The execution engine is set to .NET Core");
                            break;
                    }
                }
                return;
            }

            var host = "dotnet";
            var arguments = new List<string>(args);

            if (ConfiguredFullDotNetLauncher != null)
            {
                if (Environment.OSVersion.IsWin())
                {
                    host = ConfiguredFullDotNetLauncher;
                }
                else
                {
                    host = "mono";
                    arguments.Insert(0, ConfiguredFullDotNetLauncher);
                }
            }
            else
            {
                host = "dotnet";
                var engine = "cscs.dll";
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CSS_WINAPP")))
                    engine = "csws.dll";
                arguments.Insert(0, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), engine));
            }

            // ScriptLauncher.ShowConsole(); // interferes with Conspole.ReadKey

            if (hideConsole)
                ScriptLauncher.HideConsole();

            ScriptLauncher.Run(host, arguments.ToCmdArgs());
        }

        private static string RedirectFileName
        {
            get => Assembly.GetExecutingAssembly().Location + ".net_redirect";
        }

        private static string ConfiguredFullDotNetLauncher
        {
            get
            {
                if (!Environment.OSVersion.IsWin())
                    return null;

                if (!File.Exists(RedirectFileName))
                    return null;

                // "CSSCRIPT_DIR" is set during install with Choco
                // "CSSCRIPT_FULL_DIR" is an old attempt to differentiate install dirs for
                // .NET full and Core editions currently it is
                //  CSSCRIPT_DIR  - .NET full
                //  CSSCRIPT_ROOT - .NET Core
                var css_dir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
                if (css_dir != null)
                {
                    var launcher = Path.Combine(css_dir, "cscs.exe");
                    if (File.Exists(launcher))
                        return launcher;
                }

                return null;
            }
        }
    }
}