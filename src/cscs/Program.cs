using System;
using System.Diagnostics;
using System.Linq;
using csscript;
using CSScripting.CodeDom;

/*
ensure -cd creates dll in the right folder
ensure -cd does not creates static main
*/

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
            Runtime.GlobalIncludsDir.EnsureDir();

            if (args.Contains("-server:stop"))
                BuildServer.Stop();
            else if (args.Contains("-server") || args.Contains("-server:start"))
                BuildServer.Start();
            else
                CSExecutionClient.Run(args);
        }
    }
}