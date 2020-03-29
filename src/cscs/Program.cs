using System;
using System.Linq;
using csscript;
using CSScripting.CodeDom;

namespace cscs
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // var tt = Project.GenerateProjectFor(@"e:\PrivateData\Galos\Projects\cs-script.core\src\cscs\bin\Debug\netcoreapp3.1\.temp..cs");

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