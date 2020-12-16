using CSScripting;
using CSScriptLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Net;
using static System.Environment;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            NetCompiler.EnableLatestSyntax();

            CSScript.EvaluatorConfig.DebugBuild = true;

            var sw = Stopwatch.StartNew();

            Console.WriteLine($"Hosting runtime: .NET { (Runtime.IsCore ? "Core" : "Framework")}");
            Console.WriteLine("================\n");

            Console.WriteLine("CodeDOM");
            Test_CodeDom();
            Console.WriteLine("  first run: " + sw.ElapsedMilliseconds);
            sw.Restart();
            Test_CodeDom();
            Console.WriteLine("  next run: " + sw.ElapsedMilliseconds);
        }

        static void Test_CodeDom()
        {
            CodeDomEvaluator.CompileOnServer = true;

            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadMethod(@"public object func()
                                                   {
                                                       // return new [] {0, 5}; // C#5 syntax  (.NET Framework)
                                                       return (0,5);      // C#7+ syntax (Microsoft.Net.Compilers package)
                                                   }");

            var result = script.func();
        }

        class NetCompiler
        {
            public static string DownloadCompiler()
            {
                var packageFile = Path.GetFullPath("roslyn.zip");
                var contentDir = Path.GetFullPath("compilers");
                var packageUrl = "https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers/3.8.0";
                var compilerFile = Path.Combine(contentDir, "tools", "csc.exe");

                if (File.Exists(compilerFile))
                    return compilerFile;

                try
                {
                    Console.WriteLine("Downloading latest C# compiler...");

                    new WebClient()
                        .DownloadFile(packageUrl, packageFile);

                    ZipFile.ExtractToDirectory(packageFile, contentDir);

                    return compilerFile;
                }
                catch
                {
                    Console.WriteLine($"Cannot download '{packageUrl}' ...");
                    return null;
                }
            }

            public static void EnableLatestSyntax()
            {
                var latest_csc = DownloadCompiler();
                if (latest_csc.HasText())
                {
                    Globals.csc = latest_csc;
                }
                else
                {
                    Console.WriteLine("Using default C#5 compiler");
                }
            }
        }
    }
}