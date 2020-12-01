using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace compile_server
{
    class Program
    {
        static void Main(string[] args)
        {
            App.Log("Starting...");

            if (args.FirstOrDefault() == "-start")
            {
                if (!BuildServer.AnyRunningInstance)
                    BuildServer.Start();
            }
            else
            {
                if (!BuildServer.AnyRunningInstance)
                    Process.StartWithoutConsole("dotnet", $"{Assembly.GetExecutingAssembly().Location} -start");

                var buildLog = BuildClient.Build(args);
                Console.WriteLine(buildLog);
            }
        }
    }
}