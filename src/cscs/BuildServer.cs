using csscript;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using static System.Console;

namespace CSScripting.CodeDom
{
    public static class CscBuildServer
    {
        public static string build_server = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("build.dll");

        public static void Start()
        {
            // A simple Process.Start does not work as the child process will be linked to the parent Console
            // killed and this will mess up on whole process life time management of the child (build.dll) and
            // the grand-child process.
            //      Process.Start("dotnet", $"\"{build_server}\" -start");
            // Thus just start server with `-start` and if it is already started then it will gracefully exit
            try { "dotnet".StartWithoutConsole($"{build_server} -start"); }
            catch { /**/}
        }

        public static void Stop()
        {
            try { "dotnet".StartWithoutConsole($"{build_server} -stop"); }
            catch { /**/}
        }
    }

    public static class BuildServer
    {
        public static int serverPort = 17001;

        static public string SentRequest(string request)
        {
            using var clientSocket = new TcpClient();
            clientSocket.Connect(IPAddress.Loopback, serverPort);
            clientSocket.WriteAllBytes(request.GetBytes());
            return clientSocket.ReadAllBytes().GetString();
        }

        public static void Stop()
        {
            try
            {
                using var clientSocket = new TcpClient();
                clientSocket.Connect(IPAddress.Loopback, serverPort);
                clientSocket.WriteAllText("-exit");
            }
            catch { }
        }

        public static void Start()
        {
            Profiler.measure(">> Initialized: ", () => RoslynService.Init());

            try
            {
                var serverSocket = new TcpListener(IPAddress.Loopback, serverPort);
                serverSocket.Start();

                while (true)
                {
                    using (TcpClient clientSocket = serverSocket.AcceptTcpClient())
                    {
                        try
                        {
                            string request = clientSocket.ReadAllText();

                            if (request == "-exit")
                            {
                                try { clientSocket.WriteAllText("Bye"); } catch { }
                                break;
                            }

                            Profiler.measure(">> Processing client request: ", () =>
                            {
                                string response = RoslynService.process_build_remotelly_request(request);
                                clientSocket.WriteAllText(response);
                            });
                        }
                        catch (Exception e)
                        {
                            WriteLine(e.Message);
                        }
                    }
                }

                serverSocket.Stop();
                WriteLine(" >> exit");
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10048)
                    WriteLine(">" + e.Message);
                else
                    WriteLine(e.Message);
            }
            catch (Exception e)
            {
                WriteLine(e);
            }
        }
    }
}