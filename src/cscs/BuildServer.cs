using System;
using static System.Console;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

// using compile_server;

// using csscript;

namespace CSScripting.CodeDom
{
    // public static class CscBuildServer
    // {
    //     public static string build_server = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("build.dll");

    //     public static void Start()
    //     {
    //         // A simple Process.Start does not work as the child process will be linked to the parent Console
    //         // killed and this will mess up on whole process life time management of the child (build.dll) and
    //         // the grand-child process.
    //         //      Process.Start("dotnet", $"\"{build_server}\" -start");
    //         // Thus just start server with `-start` and if it is already started then it will gracefully exit
    //         try { "dotnet".StartWithoutConsole($"{build_server} -start"); }
    //         catch { /**/}
    //     }

    //     public static void Stop()
    //     {
    //         try { "dotnet".StartWithoutConsole($"{build_server} -stop"); }
    //         catch { /**/}
    //     }
    // }

    public static partial class BuildServer
    {
        static string csc_asm_file;

        static public string csc
        {
            get
            {
                if (csc_asm_file == null)
                {
                    // linux ~dotnet/.../3.0.100-preview5-011568/Roslyn/... (cannot find in preview)
                    // win: program_files/dotnet/sdk/<version>/Roslyn/csc.dll
                    var dotnet_root = "".GetType().Assembly.Location;

                    // find first "dotnet" parent dir by trimming till the last "dotnet" token
                    dotnet_root = String.Join(Path.DirectorySeparatorChar,
                                              dotnet_root.Split(Path.DirectorySeparatorChar)
                                                         .Reverse()
                                                         .SkipWhile(x => x != "dotnet")
                                                         .Reverse()
                                                         .ToArray());

                    var sdkDir = Path.Combine(dotnet_root, "sdk");
                    if (Directory.Exists(sdkDir)) // need to check as otherwise it will throw
                    {
                        var dirs = Directory.GetDirectories(sdkDir)
                                            .Where(dir => { var firstChar = Path.GetFileName(dir)[0]; return char.IsDigit(firstChar); })
                                            .OrderBy(x => Version.Parse(Path.GetFileName(x).Split('-').First()))
                                            .ThenBy(x => Path.GetFileName(x).Split('-').Count())
                                            .SelectMany(dir => Directory.GetDirectories(dir, "Roslyn"))
                                            .ToArray();

                        csc_asm_file = dirs.Select(dir => Path.Combine(dir, "bincore", "csc.dll"))
                                       .LastOrDefault(File.Exists);
                    }
                }
                return csc_asm_file;
            }
        }

        public static int serverPort = 17001;

        static public string Request(string request)
        {
            using (var clientSocket = new TcpClient())
            { 
                clientSocket.Connect(IPAddress.Loopback, serverPort);
                clientSocket.WriteAllBytes(request.GetBytes());
                return clientSocket.ReadAllBytes().GetString();
            }
        }

        static public string SendBuildRequest(string[] args)
        {
            try
            {
                // first arg is the compiler identifier: csc|vbc

                string request = string.Join('\n', args.Skip(1));
                string response = BuildServer.Request(request);

                return response;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        static public bool IsServerAlive()
        {
            try
            {
                BuildServer.Request("-ping");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void EnsureServerRunning()
        {
            if (!IsServerAlive())
                StartRemoteInstance();
        }

        public static void StartRemoteInstance()
        {
            try
            {
                System.Diagnostics.Process proc = new();

                proc.StartInfo.FileName = "dotnet";
                proc.StartInfo.Arguments = $"{Assembly.GetExecutingAssembly().Location} -listen";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
            }
            catch { }
        }

        public static string StopRemoteInstance()
        {
            try
            {
                return "-stop".SendTo(IPAddress.Loopback, serverPort);
            }
            catch { return "<no respone>"; }
        }

        public static string PingRemoteInstance()
        {
            try
            {
                return "-ping".SendTo(IPAddress.Loopback, serverPort);
            }
            catch { return "<no respone>"; }
        }

        public static void ListenToRequests()
        {
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

                            if (request == "-stop")
                            {
                                try { clientSocket.WriteAllText($"Terminating pid:{Process.GetCurrentProcess().Id}"); } catch { }
                                break;
                            }
                            else if (request == "-ping")
                            {
                                try { clientSocket.WriteAllText($"pid:{Process.GetCurrentProcess().Id}"); } catch { }
                            }
                            else
                            {
                                string response = CompileWithCsc(request.Split('\n'));

                                clientSocket.WriteAllText(response);
                            }
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

        static string CompileWithCsc(string[] args)
        {
            using (SimpleAsmProbing.For(Path.GetDirectoryName(csc)))
            {
                var oldOut = Console.Out;
                using StringWriter buff = new();

                Console.SetOut(buff);

                try
                {
                    AppDomain.CurrentDomain.ExecuteAssembly(csc, args);
                }
                catch (Exception e)
                {
                    return e.ToString();
                }
                finally
                {
                    Console.SetOut(oldOut);
                }
                return buff.GetStringBuilder().ToString();
            }
        }
    }
}