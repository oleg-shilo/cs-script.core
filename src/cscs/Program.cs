using csscript;
using CSScripting.CodeDom;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

/*
 TODO:

   csc_builder
     - csc location on Linux
     - test on Linux
     - migrate IPC on sockets instead of file system
     - add configurable exit on idle
     - cleanup job queue folder

   cscs
     + report using of csc_builder for WPF project
     + add configurable use of csc_builder
     + bind csc_builder to -server:exit and -server:start
     - remove old Roslyn-based build server
     - code cleanup
     + check if nuget works
     + check csc engine respects build dll and build exe

   CSSCriptLib
     - implement Engine.CodeDom
     - ensure Engine.CodeDom supports multi-scripting
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
                CscBuildServer.Stop();
            else if (args.Contains("-server") || args.Contains("-server:start"))
                CscBuildServer.Start();
            else
                CSExecutionClient.Run(args);
        }
    }
}