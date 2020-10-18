using System;
using System.Diagnostics;
using System.Linq;
using csscript;
using CSScripting.CodeDom;

namespace cscs
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //Debug.Assert(false);

            Environment.SetEnvironmentVariable("DOTNET_SHARED", typeof(string).Assembly.Location.GetDirName().GetDirName());
            Environment.SetEnvironmentVariable("WINDOWS_DESKTOP_APP", Runtime.DesktopAssembliesDir);
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