using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace compile_server
{
    static class BuildClient
    {
        static public string Build(string[] args)
        {
            var requestName = $"{Guid.NewGuid()}.rqst";
            var responseName = Path.ChangeExtension(requestName, ".resp");

            string responseFile = Path.Combine(BuildServer.JobQueueDir, responseName); ;

            try
            {
                Directory.CreateDirectory(BuildServer.JobQueueDir);
                var request = Path.Combine(BuildServer.JobQueueDir, requestName);

                // first arg is the compiler identifier: csc|vbc
                File.WriteAllLines(request, args.Skip(1));

                while (!File.Exists(responseFile))
                    Thread.Sleep(20);

                if (responseFile != null)
                    return File.ReadAllText(responseFile);
                else
                    return "Error: cannot process compile request on CS-Script build server ";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
            finally
            {
                try { if (File.Exists(responseFile)) File.Delete(responseFile); } catch { }
            }
        }
    }

    static class Process
    {
        public static void StartWithoutConsole(string app, string arguments)
        {
            System.Diagnostics.Process proc = new();

            proc.StartInfo.FileName = app;
            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
        }
    }
}