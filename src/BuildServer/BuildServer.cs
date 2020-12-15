using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace compile_server
{
    static class App
    {
        static Mutex mutex = null;
        static string mutexName = $"cs-script.core.build.{Assembly.GetExecutingAssembly().GetName().Version}";

        // Mutex will be fully disposed on process exit anyway
        static public void OnExit() => mutex.ReleaseMutex();

        static public void SignalItselfAsRunning()
        {
            IsRunning(); // will claim mutex if it's not done yet
            File.WriteAllText(Path.Combine(BuildServer.DefaultJobQueuePath, "server.pid"), System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
        }

        static public bool IsRunning()
        {
            mutex = new Mutex(true, mutexName, out bool createdNew);
            return !createdNew;
        }

        static public void Log(string message)
        {
            Console.WriteLine(message);
            // File.WriteAllText(Path.Combine(BuildServer.DefaultJobQueuePath, "server.log"),
            //     $"{System.Diagnostics.Process.GetCurrentProcess().Id}:{DateTime.Now.ToString("-s")}:{message}{Environment.NewLine}");
        }
    }

    static class BuildServer
    {
        static public bool AnyRunningInstance => App.IsRunning();

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

        public static string DefaultJobQueuePath
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                       "cs-script", "bin", "compiler", Assembly.GetExecutingAssembly().GetName().Version.ToString(), "queue");

                Directory.CreateDirectory(dir);

                return dir;
            }
        }

        static public bool ExitRequested = false;
        static public string JobQueueDir => DefaultJobQueuePath;

        static public void Start()
        {
            App.SignalItselfAsRunning();
            App.Log($"Server started ({System.Diagnostics.Process.GetCurrentProcess().Id})...");

            try
            {
                while (!ExitRequested)
                {
                    Directory.CreateDirectory(JobQueueDir);

                    bool doNotWait = false;
                    foreach (var request in Directory.GetFiles(JobQueueDir, "*.rqst"))
                    {
                        Console.WriteLine("Request: " + request);

                        try
                        {
                            var sw = Stopwatch.StartNew();
                            string response = Compile(File.ReadAllLines(request));
                            sw.Stop();

                            Console.WriteLine(sw.ElapsedMilliseconds);

                            var pendingFile = Path.ChangeExtension(request, ".pending");
                            var responseFile = Path.ChangeExtension(request, ".resp");

                            // ensure a single step response file creation so observers can
                            // detect it only when it is completely written
                            File.WriteAllText(pendingFile, response);
                            File.Move(pendingFile, responseFile, overwrite: true);
                            File.Delete(request);
                        }
                        catch
                        {
                            doNotWait = true;
                            // Console.WriteLine(e);
                        }
                    }

                    if (!doNotWait)
                        FileWatcher.WaitForCreated(JobQueueDir, "*.rqst", timeout: 5000);

                    ExitRequested = File.Exists(Path.Combine(JobQueueDir, "exit"));
                }
            }
            finally
            {
                if (File.Exists(Path.Combine(JobQueueDir, "exit")))
                    try { File.Delete(Path.Combine(JobQueueDir, "exit")); } catch { }
            }
        }

        static string Compile(string[] args)
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

    class FileWatcher
    {
        public static string WaitForCreated(string dir, string filter, int timeout)
        {
            var result = new FileSystemWatcher(dir, filter).WaitForChanged(WatcherChangeTypes.Created, timeout);
            return result.TimedOut ? null : Path.Combine(dir, result.Name);
        }
    }
}