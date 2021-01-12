using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using csscript;
using CSScripting;
using CSScripting.CodeDom;

/*
 TODO:

   csc_builder
     + port on socket IPC
     + migrate IPC on sockets instead of file system
     + exit all instances on mutex
     + implement config for port number
     - csc location on Linux
     - test on Linux
     - add configurable exit on idle

   cscs
     - code cleanup
     - VB support
     - Unify namespaces
     - Migrate app settings to json
     - remove old not used settings
     - clean help content from unused stuff
     - implement config for port number
     + remove old Roslyn-based build server
     + report using of csc_builder for WPF project
     + add configurable use of csc_builder
     + bind csc_builder to -server:exit and -server:start and read config
     + check if nuget works
     + check csc engine respects build dll and build exe

   CSSCriptLib
     - VB support
     - implement config for port number
     + XML documentation
     + tunneling compiler options
     + implement Engine.CodeDom
     + ensure Engine.CodeDom supports multi-scripting
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
                Runtime.GlobalIncludsDir?.EnsureDir();

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