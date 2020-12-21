using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CSScripting.CodeDom;

namespace compile_server
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.FirstOrDefault() == "-start")
                {
                    App.Log($"Starting remote instance...");
                    BuildServer.StartRemoteInstance();
                }
                else if (args.FirstOrDefault() == "-stop")
                {
                    App.Log($"Stopping remote instance...");
                    App.Log(BuildServer.StopRemoteInstance());
                }
                else if (args.FirstOrDefault() == "-ping")
                {
                    App.Log($"Pinging remote instance...");
                    App.Log(BuildServer.PingRemoteInstance());
                }
                else if (args.FirstOrDefault() == "-listen")
                {
                    // Debugger.Launch();
                    App.Log($"Starting server pid:{ Process.GetCurrentProcess().Id}");
                    BuildServer.ListenToRequests();
                }
                else
                {
                    // Debugger.Launch();
                    BuildServer.StartRemoteInstance();
                    var buildLog = BuildServer.SendBuildRequest(args);

                    // keep Console as app.log may be swallowing the messages
                    // and the parent process needs to read the console output
                    Console.WriteLine(buildLog);
                }
            }
            catch (Exception e)
            {
                App.Log(e.ToString());
            }
        }
    }
}