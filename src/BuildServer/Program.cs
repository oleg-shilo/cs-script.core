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

    static class App
    {
        static Mutex mutex = null;
        static string mutexName = $"cs-script.core.build.{Assembly.GetExecutingAssembly().GetName().Version}";

        // Mutex will be fully disposed on process exit anyway
        static public void OnExit() => mutex.ReleaseMutex();

        //static public void SignalItselfAsRunning()
        //{
        //    IsRunning(); // will claim mutex if it's not done yet
        //    File.WriteAllText(Path.Combine(BuildServer.DefaultJobQueuePath, "server.pid"), System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
        //}

        static public bool IsRunning()
        {
            mutex = new Mutex(true, mutexName, out bool createdNew);
            return !createdNew;
        }

        static public void Log(string message)
        {
            Console.WriteLine(message);
            // File.WriteAllText(Path.Combine(BuildServer.DefaultJobQueuePath, "server.log"),
            //     $"{System.Diagnostics.Process.GetCurrentProcess().Id}:{DateTime.Now.ToString("-s")}:{message}{Environment.NewLine}");
        }
    }
}