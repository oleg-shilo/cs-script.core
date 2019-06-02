using System;
using System.IO;
using System.Linq;

public static class Runtime
{
    static public string NuGetCacheView => "<not defined>";

    public static bool IsWin { get; } = !isLinux;

    // Note it is not about OS being exactly Linux but rather about OS having Linux type of file system.
    // For example path being case sensitive
    public static bool isLinux { get; } = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX);

    public static bool IsMono { get; } = (Type.GetType("Mono.Runtime") != null);
    public static bool IsCore { get; } = "".GetType().Assembly.Location.Split(Path.DirectorySeparatorChar).Contains("Microsoft.NETCore.App");
    public static bool IsNet { get; } = !IsMono && !IsCore;
}