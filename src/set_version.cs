using static System.Environment;
using System.IO;
using System.Linq;
using System.Xml.Linq;

// var version = "1.4.4.0-NET5-RC4";
var version = args.FirstOrDefault();

@"CSScriptLib\src\CSScriptLib\CSScriptLib.csproj".set_version(version);
@"csws\csws.csproj".set_version(version);
@"cscs\cscs.csproj".set_version(version);
@"css\Properties\AssemblyInfo.cs".set_version_old_proj_format(version);

static class methods
{
    public static void set_version_old_proj_format(this string project, string version)
    {
        version = version.strip_text();

        var content = File.ReadAllLines(project)
                          .Where(x => !x.StartsWith("[assembly: AssemblyVersion") && !x.StartsWith("[assembly: AssemblyFileVersion"))
                          .Concat(new[]{
                            $"[assembly: AssemblyVersion(\"{version}\")]{NewLine}" +
                            $"[assembly: AssemblyFileVersion(\"{version}\")]"});

        File.WriteAllLines(project, content);
    }

    public static void set_version(this string project, string version)
    {
        project.set_version("Version", version);
        project.set_version("FileVersion", version.strip_text());
        project.set_version("AssemblyVersion", version.strip_text());
    }

    public static void set_version(this string project, string elementName, string version)
    {
        var doc = XDocument.Load(project);
        doc.Descendants()
           .First(x => x.Name == elementName)
           .SetValue(version);
        doc.Save(project);
    }

    public static string strip_text(this string version)
        => new string(version.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
}