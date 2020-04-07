using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using csscript;
using CSScripting.CodeDom;

namespace cscs
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("DOTNET_SHARED", typeof(string).Assembly.Location.GetDirName().GetDirName());
            Environment.SetEnvironmentVariable("WINDOWS_DESKTOP_APP", typeof(string).Assembly.Location.GetDirName().Replace("Microsoft.NETCore.App", "Microsoft.WindowsDesktop.App"));
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