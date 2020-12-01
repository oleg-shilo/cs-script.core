using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CSScriptLib
{
    class CSExecutor
    {
        ///<summary>
        /// Contains the name of the temporary cache folder in the CSSCRIPT subfolder of Path.GetTempPath(). The cache folder is specific for every script file.
        /// </summary>
        static public string ScriptCacheDir { get; set; } = "";

        static public void SetScriptCacheDir(string scriptFile)
        {
            string newCacheDir = GetCacheDirectory(scriptFile); //this will also create the directory if it does not exist
            ScriptCacheDir = newCacheDir;
        }

        /// <summary>
        /// Generates the name of the cache directory for the specified script file.
        /// </summary>
        /// <param name="file">Script file name.</param>
        /// <returns>Cache directory name.</returns>
        public static string GetCacheDirectory(string file)
        {
            string commonCacheDir = Path.Combine(CSScript.GetScriptTempDir(), "cache");

            string cacheDir;
            string directoryPath = Path.GetDirectoryName(Path.GetFullPath(file));
            string dirHash;
            if (Runtime.IsWin)
            {
                //Win is not case-sensitive so ensure, both lower and capital case path yield the same hash
                dirHash = directoryPath.ToLower().GetHashCodeEx().ToString();
            }
            else
            {
                dirHash = directoryPath.GetHashCodeEx().ToString();
            }

            cacheDir = Path.Combine(commonCacheDir, dirHash);

            if (!Directory.Exists(cacheDir))
                try
                {
                    Directory.CreateDirectory(cacheDir);
                }
                catch (UnauthorizedAccessException)
                {
                    var parentDir = commonCacheDir;

                    if (!Directory.Exists(commonCacheDir))
                        parentDir = Path.GetDirectoryName(commonCacheDir); // GetScriptTempDir()

                    throw new Exception("You do not have write privileges for the CS-Script cache directory (" + parentDir + "). " +
                                        "Make sure you have sufficient privileges or use an alternative location as the CS-Script " +
                                        "temporary  directory (cscs -config:set=CustomTempDirectory=<new temp dir>)");
                }

            string infoFile = Path.Combine(cacheDir, "css_info.txt");
            if (!File.Exists(infoFile))
                try
                {
                    using (var sw = new StreamWriter(infoFile))
                    {
                        sw.WriteLine(Environment.Version.ToString());
                        sw.WriteLine(directoryPath);
                    }
                }
                catch
                {
                    //there can be many reasons for the failure (e.g. file is already locked by another writer),
                    //which in most of the cases does not constitute the error but rather a runtime condition
                }

            return cacheDir;
        }
    }

    /// <summary>
    /// Settings is an class that holds CS-Script application settings.
    /// </summary>
    public class Settings
    {
        public static Func<string, Settings> Load = (file) => new Settings();

        /// <summary>
        /// Gets the default configuration file path. It is a "css_config.xml" file located in the same directory where the assembly
        /// being executed is (e.g. cscs.exe).
        /// </summary>
        /// <value>
        /// The default configuration file location. Returns null if the file is not found.
        /// </value>
        public static string DefaultConfigFile
        {
            get
            {
                try
                {
                    string asm_path = Assembly.GetExecutingAssembly().Location;
                    if (asm_path.IsNotEmpty())
                        return asm_path.ChangeFileName("css_config.xml");
                }
                catch { }
                return null;
            }
        }

        /// <summary>
        /// List of directories to be used to search (probing) for referenced assemblies and script files.
        /// This setting is similar to the system environment variable PATH.
        /// </summary>
        public string[] SearchDirs { get => searchDirs.ToArray(); }

        public string DefaultRefAssemblies { get; set; }

        List<string> searchDirs { get; set; } = new List<string>();

        /// <summary>
        /// Clears the search directories.
        /// </summary>
        /// <returns></returns>
        public Settings ClearSearchDirs()
        {
            searchDirs.Clear();
            return this;
        }

        /// <summary>
        /// Adds the search directories aggregated from the unique locations of all assemblies referenced by the host application.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <returns></returns>
        public Settings AddSearchDir(string dir)
        {
            searchDirs.Add(Environment.ExpandEnvironmentVariables(dir));
            return this;
        }

        /// <summary>
        /// Adds the search dirs from host.
        /// </summary>
        /// <returns></returns>
        public Settings AddSearchDirsFromHost()
        {
            try
            {
                var dirs = new List<string>();
                foreach (var asm in Assembly.GetCallingAssembly().GetReferencedAssemblies())
                    try
                    {
                        var dir = Assembly.Load(asm).Directory(); // the asm is already loaded by the host anyway
                        if (dir.IsNotEmpty())
                            dirs.Add(dir);
                    }
                    catch { }
                searchDirs.AddRange(dirs.Distinct());
            }
            catch { }
            return this;
        }
    }

    /// <summary>
    /// Class which is implements CS-Script class library interface.
    /// </summary>
    public partial class CSScript
    {
        static string tempDir = null;

        /// <summary>
        /// Returns the name of the temporary folder in the CSSCRIPT subfolder of Path.GetTempPath().
        /// <para>Under certain circumstances it may be desirable to the use the alternative location for the CS-Script temporary files.
        /// In such cases use SetScriptTempDir() to set the alternative location.
        /// </para>
        /// </summary>
        /// <returns>Temporary directory name.</returns>
        static public string GetScriptTempDir()
        {
            if (tempDir == null)
            {
                tempDir = Environment.GetEnvironmentVariable("CSS_CUSTOM_TEMPDIR");
                if (tempDir == null)
                {
                    tempDir = Path.Combine(Path.GetTempPath(), "CSSCRIPT");
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }
                }
            }
            return tempDir;
        }

        static Dictionary<UInt32, string> dynamicScriptsAssemblies = new Dictionary<UInt32, string>();

        // /// <summary>
        // /// Compiles script code into assembly with CSExecutor and loads it in current AppDomain.
        // /// </summary>
        // /// <param name="scriptText">The script code to be compiled.</param>
        // /// <param name="tempFileExtension">The file extension of the temporary file to hold script code during compilation. This parameter may be
        // /// needed if custom CS-Script compilers rely on file extension to identify the script syntax.</param>
        // /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        // /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        // /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        // /// <returns>Compiled assembly.</returns>
        // static public Assembly LoadCode(string scriptText, string tempFileExtension, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        // {
        //     lock (typeof(CSScript))
        //     {
        //         UInt32 scriptTextCRC = 0;
        //         if (CacheEnabled)
        //         {
        //             scriptTextCRC = Crc32.Compute(Encoding.UTF8.GetBytes(scriptText));
        //             if (dynamicScriptsAssemblies.ContainsKey(scriptTextCRC))
        //                 try
        //                 {
        //                     var location = dynamicScriptsAssemblies[scriptTextCRC];
        //                     if (location.StartsWith("inmem:"))
        //                     {
        //                         string name = location.Substring("inmem:".Length);
        //                         return AppDomain.CurrentDomain.GetAssemblies().
        //                                SingleOrDefault(assembly => assembly.FullName == name);
        //                     }
        //                     else
        //                         return Assembly.LoadFrom(location);
        //                 }
        //                 catch
        //                 {
        //                     Trace.WriteLine("Cannot use cache...");
        //                 }
        //         }

        //         string tempFile = GetScriptTempFile("dynamic");
        //         if (tempFileExtension != null && tempFileExtension != "")
        //             tempFile = Path.ChangeExtension(tempFile, tempFileExtension);

        //         try
        //         {
        //             var fileLock = new Mutex(false, tempFile.Replace(Path.DirectorySeparatorChar, '|').ToLower());
        //             fileLock.WaitOne(1);
        //             // do not release mutex. The file may be needed to be locked until the
        //             // host process exits (e.g. debugging). Thus the mutex will be released by OS when the process is terminated

        //             File.WriteAllText(tempFile, scriptText);

        //             Assembly asm = Load(tempFile, assemblyFile, debugBuild, refAssemblies);

        //             string location = asm.Location();

        //             if (CacheEnabled)
        //             {
        //                 if (String.IsNullOrEmpty(location))
        //                 {
        //                     if (Environment.GetEnvironmentVariable("CSS_DISABLE_INMEM_ASM_CACHING") != "true")
        //                         location = "inmem:" + asm.FullName;
        //                 }

        //                 if (!string.IsNullOrEmpty(location))
        //                 {
        //                     if (dynamicScriptsAssemblies.ContainsKey(scriptTextCRC))
        //                         dynamicScriptsAssemblies[scriptTextCRC] = location;
        //                     else
        //                         dynamicScriptsAssemblies.Add(scriptTextCRC, location);
        //                 }
        //             }
        //             return asm;
        //         }
        //         finally
        //         {
        //             if (!debugBuild)
        //                 tempFile.FileDelete(rethrow: false);
        //             else
        //                 NoteTempFile(tempFile);
        //         }
        //     }
        // }

        /// <summary>
        /// Returns the name of the temporary file in the CSSCRIPT subfolder of Path.GetTempPath().
        /// </summary>
        /// <returns>Temporary file name.</returns>
        static public string GetScriptTempFile()
        {
            lock (typeof(CSScript))
            {
                return Path.Combine(GetScriptTempDir(), string.Format("{0}.{1}.tmp", Process.GetCurrentProcess().Id, Guid.NewGuid()));
            }
        }

        static internal string GetScriptTempFile(string subDir)
        {
            lock (typeof(CSScript))
            {
                string tempDir = Path.Combine(GetScriptTempDir(), subDir);
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                return Path.Combine(tempDir, string.Format("{0}.{1}.tmp", Process.GetCurrentProcess().Id, Guid.NewGuid()));
            }
        }

        /// <summary>
        /// Settings object containing runtime settings, which controls script compilation/execution.
        /// This is Settings class essentially is a deserialized content of the CS-Script configuration file (css_config.xml).
        /// </summary>
        static public Settings GlobalSettings = new Settings();

        /// <summary>
        /// Global instance of <see cref="CSScriptLib.RoslynEvaluator"/>. This object is to be used for
        /// dynamic loading of the  C# code by using Roslyn "compiler as service".
        /// <para>If you need to use multiple instances of th evaluator then you will need to call
        /// <see cref="CSScriptLib.IEvaluator"/>.Clone().
        /// </para>
        /// </summary>
        /// <value> The <see cref="CSScriptLib.RoslynEvaluator"/> instance.</value>
        static public RoslynEvaluator RoslynEvaluator
        {
            get
            {
                if (EvaluatorConfig.Access == EvaluatorAccess.AlwaysCreate)
                    return (RoslynEvaluator)roslynEvaluator.Value.Clone();
                else
                    return roslynEvaluator.Value;
            }
        }

        /// <summary>
        /// Controls if ScriptCache should be used when script file loading is requested (CSScript.Load(...)). If set to true and the script file was previously compiled and already loaded
        /// the script engine will use that compiled script from the cache instead of compiling it again.
        /// Note the script cache is always maintained by the script engine. The CacheEnabled property only indicates if the cached script should be used or not when CSScript.Load(...) method is called.
        /// </summary>
        public static bool CacheEnabled { get; set; } = true;

        static List<string> tempFiles;

        internal static void NoteTempFile(string file)
        {
            if (tempFiles == null)
            {
                tempFiles = new List<string>();
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnApplicationExit);
            }
            tempFiles.Add(file);
        }

        static void OnApplicationExit(object sender, EventArgs e)
        {
            Cleanup();
        }

        static internal void Cleanup()
        {
            if (tempFiles != null)
                foreach (string file in tempFiles)
                {
                    file.FileDelete(rethrow: false);
                }

            // CleanupDynamicSources(); zos
        }

        static Lazy<RoslynEvaluator> roslynEvaluator = new Lazy<RoslynEvaluator>();

        static internal string WrapMethodToAutoClass(string methodCode, bool injectStatic, bool injectNamespace, string inheritFrom = null)
        {
            var code = new StringBuilder(4096);
            code.AppendLine("//Auto-generated file")
                .AppendLine("using System;");

            bool headerProcessed = false;

            string line;

            using (StringReader sr = new StringReader(methodCode))
                while ((line = sr.ReadLine()) != null)
                {
                    if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                    {
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("//") && trimmed != "") //not comments or empty line
                        {
                            headerProcessed = true;

                            if (injectNamespace)
                            {
                                code.AppendLine("namespace Scripting")
                                    .AppendLine("{");
                            }

                            if (inheritFrom != null)
                                code.AppendLine($"   public class {DynamicWrapperClassName} : " + inheritFrom);
                            else
                                code.AppendLine($"   public class {DynamicWrapperClassName}");

                            code.AppendLine("   {");
                            string[] tokens = line.Split("\t ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);

                            if (injectStatic)
                            {
                                //IE "unsafe public static"
                                if (!tokens.Contains("static"))
                                    code.AppendLine("   static");
                            }

                            if (!tokens.Contains("public"))
                                code.AppendLine("   public");
                        }
                    }

                    code.AppendLine(line);
                }

            code.AppendLine("   }");
            if (injectNamespace)
                code.AppendLine("}");

            return code.ToString();
        }
    }
}