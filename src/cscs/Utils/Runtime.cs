using System;
using System.IO;
using System.Linq;

#if class_lib

namespace CSScriptLib
#else

namespace csscript
#endif
{
    /// <summary>
    /// A class that hosts the most common properties of the runtime environment.
    /// </summary>
    public static class Runtime
    {
        /// <summary>
        /// Gets the nuget cache path in the form displayable in Console.
        /// </summary>
        /// <value>
        /// The nu get cache view.
        /// </value>
        static public string NuGetCacheView => "<not defined>";

        /// <summary>
        /// Gets a value indicating whether the host OS Windows.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the host OS is Windows; otherwise, <c>false</c>.
        /// </value>
        public static bool IsWin => !IsLinux;

        /// <summary>
        /// Note it is not about OS being exactly Linux but rather about OS having Linux type of file system.
        /// For example path being case sensitive
        /// </summary>
        public static bool IsLinux { get; } = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX);

        /// <summary>
        /// Gets a value indicating whether the runtime is  core.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the runtime is  core; otherwise, <c>false</c>.
        /// </value>
        public static bool IsCore { get; } = "".GetType().Assembly.Location.Split(Path.DirectorySeparatorChar).Contains("Microsoft.NETCore.App");

        static internal string CustomCommandsDir
            => Environment.SpecialFolder.CommonApplicationData.GetPath()
                                        .PathJoin("cs-script", "commands")
                                        .EnsureDir();

        static internal string GlobalIncludsDir
        {
            get
            {
                var globalIncluds = Environment.GetEnvironmentVariable("CSSCRIPT_INC");
                if (globalIncluds.IsNotEmpty())
                    return globalIncluds.EnsureDir();

                return Environment.SpecialFolder.CommonApplicationData.GetPath()
                                            .PathJoin("cs-script", "inc")
                                            .With(x =>
                                            {
                                                Environment.SetEnvironmentVariable("CSSCRIPT_INC", x, EnvironmentVariableTarget.User);
                                                return x;
                                            })
                                            .EnsureDir();
            }
        }

        /// <summary>
        /// Returns path to the `Microsoft.WindowsDesktop.App` shared assemblies of the compatible runtime version.
        /// <para>Note, there is no warranty that the dotnet dedktop assemblies belong to the same distro version as dotnet Core:
        /// <para> - C:\Program Files\dotnet\shared\Microsoft.NETCore.App\5.0.0-rc.1.20451.14</para>
        /// <para> - C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\5.0.0-rc.1.20452.2</para>
        /// </para>
        /// </summary>
        public static string DesktopAssembliesDir
        {
            get
            {
                // There is no warranty that the dotnet dedktop assemblies belong to the same distro version as dotnet Core:
                // C:\Program Files\dotnet\shared\Microsoft.NETCore.App\5.0.0-rc.1.20451.14
                // C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\5.0.0-rc.1.20452.2
                var netCoreDir = typeof(string).Assembly.Location.GetDirName();
                var dir = netCoreDir.Replace("Microsoft.NETCore.App", "Microsoft.WindowsDesktop.App");

                if (dir.DirExists())
                    return dir; // Microsoft.WindowsDesktop.App and Microsoft.NETCore.App are of teh same version

                var desiredVersion = netCoreDir.GetFileName();

                int howSimilar(string stringA, string stringB)
                {
                    var maxSimilariry = Math.Min(stringA.Length, stringB.Length);

                    for (int i = 0; i < maxSimilariry; i++)
                        if (stringA[i] != stringB[i])
                            return i;

                    return maxSimilariry;
                }

                var allDesktopVersionsRootDir = dir.GetDirName();

                var allInstalledVersions = Directory.GetDirectories(allDesktopVersionsRootDir)
                                                    .Select(d => new
                                                    {
                                                        Path = d,
                                                        Version = d.GetFileName(),
                                                        SimialrityIndex = howSimilar(d.GetFileName(), desiredVersion)
                                                    })
                                                    .OrderByDescending(x => x.SimialrityIndex);

                return allInstalledVersions.FirstOrDefault()?.Path;
            }
        }
    }
}