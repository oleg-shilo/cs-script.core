using System;
using System.Diagnostics;
using System.Linq;
using csscript;
using CSScripting.CodeDom;

namespace cscs
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("css_nuget", null);

            if (args.Contains("-server:stop"))
                BuildServer.Stop();
            else if (args.Contains("-server") || args.Contains("-server:start"))
                BuildServer.Start();
            else
                CSExecutionClient.Run(args);
        }
    }
}