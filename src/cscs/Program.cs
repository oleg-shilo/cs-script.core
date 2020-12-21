using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using csscript;
using CSScripting;
using CSScripting.CodeDom;

/*
 TODO:

   csc_builder
     + port on socket IPC
     - csc location on Linux
     - test on Linux
     - migrate IPC on sockets instead of file system
     - add configurable exit on idle
     - cleanup job queue folder
     - implement config for port number

   cscs
     - remove old Roslyn-based build server
     - code cleanup
     - VB support
     - Unify namespaces
     - Migrate app settings to json
     - remove old not used settings
     - clean help content from unused stuff
     + report using of csc_builder for WPF project
     + add configurable use of csc_builder
     + bind csc_builder to -server:exit and -server:start
     + check if nuget works
     + check csc engine respects build dll and build exe

   CSSCriptLib
     - VB support
     - tunneling compiler options
     - XML documentation
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
            try
            {
                Environment.SetEnvironmentVariable("DOTNET_SHARED", typeof(string).Assembly.Location.GetDirName().GetDirName());
                Environment.SetEnvironmentVariable("WINDOWS_DESKTOP_APP", Runtime.DesktopAssembliesDir);
                Environment.SetEnvironmentVariable("css_nuget", null);
                Runtime.GlobalIncludsDir.EnsureDir();

                if (args.Contains("-server:stop"))
                    Globals.StopBuildServer();
                else if (args.Contains("-server:start"))
                    Globals.StartBuildServer();
                else
                    CSExecutionClient.Run(args);

                Process.GetCurrentProcess().Kill(); // some background monitors may keep the app alive too long
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}