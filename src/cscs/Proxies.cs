using csscript;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CSScripting.CodeDom
{
    public enum DefaultCompilerRuntime
    {
        Standard,
        Host
    }

    public class CSharpCompiler : ICodeCompiler
    {
        public static ICodeCompiler Create()
        {
            return new CSharpCompiler();
        }

        public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit)
        {
            throw new NotImplementedException();
        }

        public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
        {
            throw new NotImplementedException();
        }

        public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
        {
            return CompileAssemblyFromFileBatch(options, new[] { fileName });
        }

        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            throw new NotImplementedException();
        }

        static string dotnet { get; } = Runtime.IsCore ?
                                       "dotnet" : Process.GetCurrentProcess().MainModule.FileName;

        static string InitBuildTools(string fileType)
        {
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var cache_root = cache_dir.GetDirName();
            var build_root = cache_root.GetDirName().PathJoin("build").EnsureDir();

            (string projectName, string language) = fileType.MapValue((".cs", to => ("build.csproj", "C#")),
                                                                      (".vb", to => ("build.vbproj", "VB")));

            var proj_template = build_root.PathJoin($"build{fileType}proj");

            if (!File.Exists(proj_template))
            {
                Utils.Run("dotnet", $"new console -lang {language}", build_root);
                build_root.PathJoin($"Program{fileType}").DeleteIfExists();
                Directory.Delete(build_root.PathJoin("obj"), true);
            }

            if (!File.Exists(proj_template)) // sdk may not be available
            {
                File.WriteAllLines(proj_template, new[]
                {
                    "<Project Sdk=\"Microsoft.NET.Sdk\">",
                    "  <PropertyGroup>",
                    "    <OutputType>Exe</OutputType>",
                    "    <TargetFramework>netcoreapp3.1</TargetFramework>",
                    "  </PropertyGroup>",
                    "</Project>"
                });
            }

            return proj_template;
        }

        public static DefaultCompilerRuntime DefaultCompilerRuntime = DefaultCompilerRuntime.Host;

        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            switch (CSExecutor.options.compilerEngine)
            {
                case Directives.compiler_roslyn:
                    return RoslynService.CompileAssemblyFromFileBatch_with_roslyn(options, fileNames);

                case Directives.compiler_dotnet:
                    return CompileAssemblyFromFileBatch_with_Build(options, fileNames);

                case Directives.compiler_csc:
                    return CompileAssemblyFromFileBatch_with_Csc(options, fileNames);

                default:
                    // return RoslynService.CompileAssemblyFromFileBatch_with_roslyn(options, fileNames);
                    // return CompileAssemblyFromFileBatch_with_Csc(options, fileNames);
                    return CompileAssemblyFromFileBatch_with_Build(options, fileNames);
            }
        }

        static public string csc_dll
        {
            get
            {
                // linux ~dotnet/.../3.0.100-preview5-011568/Roslyn/... (cannot find in preview)
                // win: program_files/dotnet/sdk/<version>/Roslyn/csc.exe
                var dotnet_root = "".GetType().Assembly.Location;

                // find first "dotnet" parent dir by trimming till the last "dotnet" token
                dotnet_root = dotnet_root.Split(Path.DirectorySeparatorChar)
                                         .Reverse()
                                         .SkipWhile(x => x != "dotnet")
                                         .Reverse()
                                         .JoinBy(Path.DirectorySeparatorChar.ToString());

                if (dotnet_root.PathJoin("sdk").DirExists()) // need to check as otherwise it will throw
                {
                    var dirs = dotnet_root.PathJoin("sdk")
                                          .PathGetDirs("*")
                                          .Where(dir => char.IsDigit(dir.GetFileName()[0]))
                                          // .Where(dir => !dir.GetFileName().Contains('-'))            // ignoring all preview
                                          .OrderBy(x => Version.Parse(x.GetFileName().Split('-').First()))
                                          .SelectMany(dir => dir.PathGetDirs("Roslyn"))
                                          .ToArray();
                    var csc_exe = dirs.Select(dir => dir.PathJoin("bincore", "csc.dll"))
                                      .LastOrDefault(File.Exists);
                    return csc_exe;
                }
                return null;
            }
        }

        CompilerResults CompileAssemblyFromFileBatch_with_Csc(CompilerParameters options, string[] fileNames)
        {
            // C:\Program Files\dotnet\sdk\1.1.10\Roslyn\bincore\csc.dll

            string projectName = fileNames.First().GetFileName();

            var engine_dir = this.GetType().Assembly.Location.GetDirName();
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = cache_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir()
                     .EnsureDir();

            var sources = new List<string>();

            fileNames.ForEach((string source) =>
                {
                    // As per dotnet.exe v2.1.26216.3 the pdb get generated as PortablePDB, which is the only format that is supported
                    // by both .NET debugger (VS) and .NET Core debugger (VSCode).

                    // However PortablePDB does not store the full source path but file name only (at least for now). It works fine in typical
                    // .Core scenario where the all sources are in the root directory but if they are not (e.g. scripting or desktop app) then
                    // debugger cannot resolve sources without user input.

                    // The only solution (ugly one) is to inject the full file path at startup with #line directive

                    var new_file = build_dir.PathJoin(source.GetFileName());
                    var sourceText = $"#line 1 \"{source}\"{Environment.NewLine}" + File.ReadAllText(source);
                    File.WriteAllText(new_file, sourceText);
                    sources.Add(new_file);
                });

            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                             .Where(Path.IsPathRooted)
                                                             .Where(asm => asm.GetDirName() != engine_dir)
                                                             .ToList();

            if (CSExecutor.options.enableDbgPrint)
                ref_assemblies.Add(Assembly.GetExecutingAssembly().Location());

            var refs = new StringBuilder();
            var assembly = build_dir.PathJoin(projectName + ".dll");

            var result = new CompilerResults();

            if (!options.GenerateExecutable || !Runtime.IsCore || DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
            {
                // todo
            }

            //----------------------------

            //pseudo-gac as .NET core does not support GAC but rather common assemblies.
            var gac = typeof(string).Assembly.Location.GetDirName();

            var refs_args = "";
            var source_args = "";

            var common_args = "/utf8output /nostdlib+ ";
            if (options.GenerateExecutable)
                common_args += "/t:exe ";
            else
                common_args += "/t:library ";

            if (options.IncludeDebugInformation)
                common_args += "/debug:portable ";  // on .net full it is "/debug+"

            if (options.CompilerOptions.IsNotEmpty())
                common_args += $"{options.CompilerOptions} ";

            common_args += "-define:TRACE;NETCORE;CS_SCRIPT";

            var gac_asms = Directory.GetFiles(gac, "System.*.dll").ToList();
            gac_asms.AddRange(Directory.GetFiles(gac, "netstandard.dll"));
            // Microsoft.DiaSymReader.Native.amd64.dll is a native dll
            gac_asms.AddRange(Directory.GetFiles(gac, "Microsoft.*.dll").Where(x => !x.Contains("Native")));

            foreach (string file in gac_asms)
                refs_args += $"/r:\"{file}\" ";

            foreach (string file in ref_assemblies)
                refs_args += $"/r:\"{file}\" ";

            foreach (string file in sources)
                source_args += $"\"{file}\" ";

            var cmd = $@"""{csc_dll}"" {common_args} {refs_args} {source_args} /out:""{assembly}""";
            //----------------------------

            Profiler.get("compiler").Start();
            result.NativeCompilerReturnValue = dotnet.Run(cmd, build_dir, x => result.Output.Add(x));
            Profiler.get("compiler").Stop();

            if (CSExecutor.options.verbose)
                Console.WriteLine("    csc.dll: " + Profiler.get("compiler").Elapsed);

            result.ProcessErrors();

            result.Errors
                  .ForEach(x =>
                  {
                      // by default x.FileName is a file name only
                      x.FileName = fileNames.FirstOrDefault(f => f.EndsWith(x.FileName ?? "")) ?? x.FileName;
                  });

            if (result.NativeCompilerReturnValue == 0 && File.Exists(assembly))
            {
                result.PathToAssembly = options.OutputAssembly;
                File.Copy(assembly, result.PathToAssembly, true);

                if (options.IncludeDebugInformation)
                    File.Copy(assembly.ChangeExtension(".pdb"),
                              result.PathToAssembly.ChangeExtension(".pdb"),
                              true);
            }
            else
            {
                if (result.Errors.IsEmpty())
                {
                    // unknown error; e.g. invalid compiler params
                    result.Errors.Add(new CompilerError { ErrorText = "Unknown compiler error" });
                }
            }

            build_dir.DeleteDir();

            return result;
        }

        internal static string CreateProject(CompilerParameters options, string[] fileNames, string outDir = null)
        {
            string projectName = fileNames.First().GetFileName();
            string projectShortName = Path.GetFileNameWithoutExtension(projectName);

            var template = InitBuildTools(Path.GetExtension(fileNames.First().GetFileName()));

            var out_dir = outDir ?? CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = out_dir.PathJoin(".build", projectName);

            build_dir.DeleteDir()
                     .EnsureDir();

            //  <Project Sdk ="Microsoft.NET.Sdk">
            //    <PropertyGroup>
            //      <OutputType>Exe</OutputType>
            //      <TargetFramework>netcoreapp3.1</TargetFramework>
            //    </PropertyGroup>
            //  </Project>
            var project_element = XElement.Parse(File.ReadAllText(template));

            var compileConstantsDelimiter = ";";
            if (projectName.GetExtension().SameAs(".vb"))
                compileConstantsDelimiter = ",";

            project_element.Add(new XElement("PropertyGroup",
                                    new XElement("DefineConstants", new[] { "TRACE", "NETCORE", "CS_SCRIPT" }.JoinBy(compileConstantsDelimiter))));

            if (!options.GenerateExecutable || !Runtime.IsCore || DefaultCompilerRuntime == DefaultCompilerRuntime.Standard)
            {
                project_element.Element("PropertyGroup")
                               .Element("OutputType")
                               .Remove();
            }

            // In .NET all references including GAC assemblies must be passed to the compiler.
            // In .NET Core this creates a problem as the compiler does not expect any default (shared)
            // assemblies to be passed. So we do need to exclude them.
            // Note: .NET project that uses 'standard' assemblies brings facade/full .NET Core assemblies in the working folder (engine dir)
            //
            // Though we still need to keep shared assembly resolving in the host as the future compiler
            // require ALL ref assemblies to be pushed to the compiler.

            bool not_in_engine_dir(string asm) => (asm.GetDirName() != Assembly.GetExecutingAssembly().Location.GetDirName());

            var ref_assemblies = options.ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                             .Where(Path.IsPathRooted)
                                                             .Where(not_in_engine_dir)
                                                             .ToList();

            void setTargetFremeworkWin() => project_element.Element("PropertyGroup")
                                                           .SetElementValue("TargetFramework", "net5.0-windows");

            bool refWinForms = ref_assemblies.Any(x => x.EndsWith("System.Windows.Forms") ||
                                                       x.EndsWith("System.Windows.Forms.dll"));
            if (refWinForms)
            {
                setTargetFremeworkWin();
                project_element.Element("PropertyGroup")
                               .Add(new XElement("UseWindowsForms", "true"));
            }

            var refWpf = options.ReferencedAssemblies.Any(x => x.EndsWith("PresentationFramework") ||
                                                               x.EndsWith("PresentationFramework.dll"));
            if (refWpf)
            {
                setTargetFremeworkWin();
                Environment.SetEnvironmentVariable("UseWPF", "true");
                project_element.Element("PropertyGroup")
                               .Add(new XElement("UseWPF", "true"));
            }

            if (CSExecutor.options.enableDbgPrint)
                ref_assemblies.Add(Assembly.GetExecutingAssembly().Location());

            void CopySourceToBuildDir(string source)
            {
                // As per dotnet.exe v2.1.26216.3 the pdb get generated as PortablePDB, which is the only format that is supported
                // by both .NET debugger (VS) and .NET Core debugger (VSCode).

                // However PortablePDB does not store the full source path but file name only (at least for now). It works fine in typical
                // .Core scenario where the all sources are in the root directory but if they are not (e.g. scripting or desktop app) then
                // debugger cannot resolve sources without user input.

                // The only solution (ugly one) is to inject the full file path at startup with #line directive. And loose the possibility
                // to use path-based source files in the project file instead of copying all files in the build dir as we do.

                var new_file = build_dir.PathJoin(source.GetFileName());
                var sourceText = File.ReadAllText(source);
                if (!source.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                    sourceText = $"#line 1 \"{source}\"{Environment.NewLine}" + sourceText;
                File.WriteAllText(new_file, sourceText);
            }

            if (ref_assemblies.Any())
            {
                var refs1 = new XElement("ItemGroup");
                project_element.Add(refs1);

                foreach (string asm in ref_assemblies)
                {
                    refs1.Add(new XElement("Reference",
                                  new XAttribute("Include", asm.GetFileName()),
                                  new XElement("HintPath", asm)));
                }
            }

            var linkSources = true;
            if (linkSources)
            {
                var includs = new XElement("ItemGroup");
                project_element.Add(includs);
                fileNames.ForEach(x =>
                {
                    // <Compile Include="..\..\..\cscs\fileparser.cs" Link="fileparser.cs" />

                    if (x.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                        includs.Add(new XElement("Page",
                                        new XAttribute("Include", x),
                                        new XAttribute("Link", Path.GetFileName(x)),
                                        new XElement("Generator", "MSBuild:Compile")));
                    else
                        includs.Add(new XElement("Compile",
                                        new XAttribute("Include", x),
                                        new XAttribute("Link", Path.GetFileName(x))));
                });
            }
            else
                fileNames.ForEach(CopySourceToBuildDir);

            var projectFile = build_dir.PathJoin(projectShortName + Path.GetExtension(template));
            File.WriteAllText(projectFile, project_element.ToString());

            var solution = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.30608.117
MinimumVisualStudioVersion = 10.0.40219.1
Project(`{9A19103F-16F7-4668-BE54-9A1E7A4F7556}`) = `{proj_name}`, `{proj_name}.csproj`, `{03A7169D-D1DD-498A-86CD-7C9587D3DBDD}`
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {03A7169D-D1DD-498A-86CD-7C9587D3DBDD}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {03A7169D-D1DD-498A-86CD-7C9587D3DBDD}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {03A7169D-D1DD-498A-86CD-7C9587D3DBDD}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{03A7169D-D1DD-498A-86CD-7C9587D3DBDD}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
    GlobalSection(ExtensibilityGlobals) = postSolution
        SolutionGuid = {629108FC-1E4E-4A2B-8D8E-159E40FF5950}
    EndGlobalSection
EndGlobal".Replace("`", "\"").Replace("{proj_name}", projectFile.GetFileNameWithoutExtension());
            File.WriteAllText(projectFile.ChangeExtension(".sln"), solution);

            return projectFile;
        }

        CompilerResults CompileAssemblyFromFileBatch_with_Build(CompilerParameters options, string[] fileNames)
        {
            var projectFile = CreateProject(options, fileNames);

            var output = "bin";
            var build_dir = projectFile.GetDirName();

            var assembly = build_dir.PathJoin(output, projectFile.GetFileNameWithoutExtension() + ".dll");

            var result = new CompilerResults();

            var config = options.IncludeDebugInformation ? "--configuration Debug" : "--configuration Release";

            Profiler.get("compiler").Start();
            result.NativeCompilerReturnValue = Utils.Run(dotnet, $"build {config} -o {output} {options.CompilerOptions}", build_dir, x => result.Output.Add(x), x => Console.WriteLine("error> " + x));
            Profiler.get("compiler").Stop();

            if (CSExecutor.options.verbose)
            {
                var timing = result.Output.FirstOrDefault(x => x.StartsWith("Time Elapsed"));
                if (timing != null)
                    Console.WriteLine("    dotnet: " + timing);
            }

            result.ProcessErrors();

            result.Errors
                  .ForEach(x =>
                  {
                      // by default x.FileName is a file name only
                      x.FileName = fileNames.FirstOrDefault(f => f.EndsWith(x.FileName ?? "")) ?? x.FileName;
                  });

            if (result.NativeCompilerReturnValue == 0 && File.Exists(assembly))
            {
                result.PathToAssembly = options.OutputAssembly;
                if (options.GenerateExecutable)
                {
                    File.Copy(assembly.ChangeExtension(".runtimeconfig.json"), result.PathToAssembly.ChangeExtension(".runtimeconfig.json"), true);
                    File.Copy(assembly.ChangeExtension(".exe"), result.PathToAssembly.ChangeExtension(".exe"), true);
                    File.Copy(assembly.ChangeExtension(".dll"), result.PathToAssembly.ChangeExtension(".dll"), true);
                }
                else
                {
                    File.Copy(assembly, result.PathToAssembly, true);
                }

                if (options.IncludeDebugInformation)
                    File.Copy(assembly.ChangeExtension(".pdb"),
                              result.PathToAssembly.ChangeExtension(".pdb"),
                              true);
            }
            else
            {
                if (result.Errors.IsEmpty())
                {
                    // unknown error; e.g. invalid compiler params
                    result.Errors.Add(new CompilerError { ErrorText = "Unknown compiler error" });
                }
            }

            build_dir.DeleteDir();

            return result;
        }

        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            return CompileAssemblyFromFileBatch(options, new[] { source });
        }
    }

    public interface ICodeCompiler
    {
        CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit);

        CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits);

        CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName);

        CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames);

        CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source);

        CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources);
    }

    public class CompilerResults
    {
        public TempFileCollection TempFiles { get; set; } = new TempFileCollection();
        public List<string> ProbingDirs { get; set; } = new List<string>();
        public Assembly CompiledAssembly { get; set; }
        public List<CompilerError> Errors { get; set; } = new List<CompilerError>();
        public List<string> Output { get; set; } = new List<string>();
        public string PathToAssembly { get; set; }
        public int NativeCompilerReturnValue { get; set; }

        internal void ProcessErrors()
        {
            var isErrroSection = true;

            // only dotnet has a distinctive error message that separates "info"
            // and "error" section. It is particularly important to process only
            // the "error" section as dotnet compiler prints the same errors in
            // both of these sections.
            if (CSExecutor.options.compilerEngine == null || CSExecutor.options.compilerEngine == Directives.compiler_dotnet)
                isErrroSection = false;

            // Build succeeded.
            foreach (var line in Output)
            {
                if (!isErrroSection)
                {
                    // MSBUILD : error MSB1001: Unknown switch.
                    if (line.StartsWith("Build FAILED.") || line.StartsWith("Build succeeded."))
                        isErrroSection = true;

                    if (line.Contains("CSC : error ") || line.Contains("): error ") || line.StartsWith("error CS") || line.StartsWith("vbc : error BC") || line.Contains("MSBUILD : error "))
                    {
                        var error = CompilerError.Parser(line);
                        if (error != null)
                            Errors.Add(error);
                    }
                }
                else
                {
                    if (line.IsNotEmpty())
                    {
                        var error = CompilerError.Parser(line);
                        if (error != null)
                            Errors.Add(error);
                    }
                }
            }
        }
    }

    public static class ProxyExtensions
    {
        public static bool HasErrors(this List<CompilerError> items) => items.Any(x => !x.IsWarning);
    }

    public class CodeCompileUnit
    {
    }

    public class TempFileCollection
    {
        public List<string> Items { get; set; } = new List<string>();

        public void Clear() => Items.ForEach(File.Delete);
    }

    public class CompilerError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string ErrorNumber { get; set; }
        public string ErrorText { get; set; }
        public bool IsWarning { get; set; }
        public string FileName { get; set; }

        public static CompilerError Parser(string compilerOutput)
        {
            // C:\Program Files\dotnet\sdk\2.1.300-preview1-008174\Sdks\Microsoft.NET.Sdk\build\Microsoft.NET.ConflictResolution.targets(59,5): error MSB4018: The "ResolvePackageFileConflicts" task failed unexpectedly. [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // script.cs(11,8): error CS1029: #error: 'this is the error...' [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // script.cs(10,10): warning CS1030: #warning: 'DEBUG is defined' [C:\Users\%username%\AppData\Local\Temp\csscript.core\cache\1822444284\.build\script.cs\script.csproj]
            // MSBUILD : error MSB1001: Unknown switch.

            if (compilerOutput.StartsWith("error CS") ||
                compilerOutput.StartsWith("vbc : error BC"))
                compilerOutput = "(0,0): " + compilerOutput;

            bool isError = compilerOutput.Contains("): error ");
            bool isWarning = compilerOutput.Contains("): warning ");
            bool isBuildError = compilerOutput.Contains("MSBUILD : error", StringComparison.OrdinalIgnoreCase) ||
                                compilerOutput.Contains("vbc : error", StringComparison.OrdinalIgnoreCase) ||
                                compilerOutput.Contains("CSC : error", StringComparison.OrdinalIgnoreCase);

            if (isBuildError)
            {
                var parts = compilerOutput.Replace("MSBUILD : error ", "", StringComparison.OrdinalIgnoreCase)
                                          .Replace("CSC : error ", "", StringComparison.OrdinalIgnoreCase)
                                          .Split(":".ToCharArray(), 2);
                return new CompilerError
                {
                    ErrorText = parts.Last().Trim(),        // MSBUILD error: Unknown switch.
                    ErrorNumber = parts.First()                         // MSB1001
                };
            }
            else if (isWarning || isError)
            {
                var result = new CompilerError();

                var rx = new Regex(@".*\(\d+\,\d+\)\:");
                var match = rx.Match(compilerOutput, 0);
                if (match.Success)
                {
                    try
                    {
                        var m = Regex.Match(match.Value, @"\(\d+\,\d+\)\:");

                        var location_items = m.Value.Substring(1, m.Length - 3).Split(separator: ',').ToArray();
                        var description_items = compilerOutput.Substring(match.Value.Length).Split(":".ToArray(), 2);

                        result.ErrorText = description_items.Last();                            // #error: 'this is the error...'
                        result.ErrorNumber = description_items.First().Split(' ').Last();       // CS1029
                        result.IsWarning = isWarning;
                        result.FileName = match.Value.Substring(0, m.Index);                    // cript.cs
                        result.Line = int.Parse(location_items[0]);                             // 11
                        result.Column = int.Parse(location_items[1]);                           // 8

                        int desc_end = result.ErrorText.LastIndexOf("[");
                        if (desc_end != -1)
                            result.ErrorText = result.ErrorText.Substring(0, desc_end);

                        return result;
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e);
                    }
                }
            }
            return null;
        }
    }

    public class CompilerParameters
    {
        public List<string> LinkedResources { get; } = new List<string>();
        public List<string> EmbeddedResources { get; } = new List<string>();
        public string Win32Resource { get; set; }
        public string CompilerOptions { get; set; }
        public int WarningLevel { get; set; }
        public bool TreatWarningsAsErrors { get; set; }
        public bool IncludeDebugInformation { get; set; }
        public string OutputAssembly { get; set; }
        public IntPtr UserToken { get; set; }
        public string MainClass { get; set; }
        public List<string> ReferencedAssemblies { get; } = new List<string>();
        public bool GenerateInMemory { get; set; }

        // controls if the compiled assembly has static mainand supports top level class
        public bool GenerateExecutable { get; set; }

        // Controls if the actiual executable needs to be build
        public bool BoildExe { get; set; }

        public string CoreAssemblyFileName { get; set; }
        public TempFileCollection TempFiles { get; set; }
    }
}