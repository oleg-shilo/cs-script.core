#region License...

//-----------------------------------------------------------------------------
// Date:	20/12/15	Time: 9:00
// Module:	CSScriptLib.Eval.Roslyn.cs
//
// This module contains the definition of the Roslyn Evaluator class. Which wraps the common functionality
// of the Mono.CScript.Evaluator class (compiler as service)
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2016 Oleg Shilo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------

#endregion License...

using csscript;
using CSScripting.CodeDom;
using CSScripting;

//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp.Scripting
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Scripting;

namespace CSScriptLib
{
    public class CodeDomEvaluator : EvaluatorBase<CodeDomEvaluator>, IEvaluator
    {
        public static bool CompileOnServer = true;

        protected override string EngineName => "CodeDom evaluator (csc.exe)";

        protected override void Validate(CompileInfo info)
        {
            if (info != null && info.RootClass != Globals.RootClassName)
                throw new CSScriptException("CompileInfo.RootClass property should only be used with Roslyn evaluator as " +
                    "it addresses the limitation associated with Roslyn. Specifically wrapping ALL scripts in the illegally " +
                    "named parent class. You are using CodeDomEvaluator so you should not set CompileInfo.RootClass to any custom value");
        }

        override protected (byte[] asm, byte[] pdb) Compile(string scriptText, string scriptFile, CompileInfo info)
        {
            // Debug.Assert(false);
            var tempScriptFile = "";
            try
            {
                if (scriptFile == null)
                {
                    tempScriptFile = CSScript.GetScriptTempFile();
                    File.WriteAllText(tempScriptFile, scriptText);
                }

                var project = Project.GenerateProjectFor(tempScriptFile);
                var refs = project.Refs.Concat(this.GetReferencedAssembliesFiles()).Distinct().ToArray();

                (byte[], byte[]) result = CompileAssemblyFromFileBatch_with_Csc(project.Files, refs, info?.AssemblyFile, this.IsDebug);

                return result;
            }
            finally
            {
                if (this.IsDebug)
                    CSScript.NoteTempFile(tempScriptFile);
                else
                    tempScriptFile.FileDelete(rethrow: false);

                CSScript.StartPurgingOldTempFiles();
            }
        }

        static string dotnet = "dotnet";

        (byte[] asm, byte[] pdb) CompileAssemblyFromFileBatch_with_Csc(string[] fileNames,
                                                                       string[] ReferencedAssemblies,
                                                                       string outAssembly,
                                                                       bool IncludeDebugInformation)
        {
            string projectName = fileNames.First().GetFileName();

            var engine_dir = this.GetType().Assembly.Location.GetDirName();
            var cache_dir = CSExecutor.ScriptCacheDir; // C:\Users\user\AppData\Local\Temp\csscript.core\cache\1822444284
            var build_dir = cache_dir.PathJoin(".build", projectName);

            try
            {
                build_dir.DeleteDir()
                         .EnsureDir();

                var sources = new List<string>(fileNames); // sources may need to hold more than fileNames

                var ref_assemblies = ReferencedAssemblies.Where(x => !x.IsSharedAssembly())
                                                         .Where(Path.IsPathRooted)
                                                         .ToList();

                var refs = new StringBuilder();
                var assembly = build_dir.PathJoin(projectName + ".dll");

                var result = new CompilerResults();

                //pseudo-gac as .NET core does not support GAC but rather common assemblies.
                var gac = typeof(string).Assembly.Location.GetDirName();

                var refs_args = new List<string>();
                var source_args = new List<string>();
                var common_args = new List<string>();

                common_args.Add("/utf8output");
                common_args.Add("/nostdlib+");
                common_args.Add("-t:library");

                // common_args.Add("/t:exe"); // need always build exe so "top-class" feature is supported even when building dlls

                if (IncludeDebugInformation)
                    common_args.Add("/debug:portable");  // on .net full it is "/debug+"

                common_args.Add("-define:TRACE;NETCORE;CS_SCRIPT");

                if (Runtime.IsCore)
                {
                    var gac_asms = Directory.GetFiles(gac, "System.*.dll").ToList();
                    gac_asms.AddRange(Directory.GetFiles(gac, "netstandard.dll"));
                    // Microsoft.DiaSymReader.Native.amd64.dll is a native dll
                    gac_asms.AddRange(Directory.GetFiles(gac, "Microsoft.*.dll").Where(x => !x.Contains("Native")));

                    foreach (string file in gac_asms.Concat(ref_assemblies).Distinct())
                        refs_args.Add($"/r:\"{file}\"");
                }
                else
                {
                    // foreach (string file in ref_assemblies)
                    // refs_args.Add($"/r:\"{file}\"");
                    refs_args.Add($"/r:\"System.Design.dll\"");
                    refs_args.Add($"/r:\"mscorlib.dll\"");
                }

                foreach (string file in sources)
                    source_args.Add($"\"{file}\"");

                string cmd;

                if (Runtime.IsCore)
                {
                    if (CompileOnServer && Globals.BuildServerIsDeployed)
                    {
                        dotnet.RunAsync($"\"{Globals.build_server}\" -start");

                        cmd = $@"""{Globals.build_server}"" csc {common_args.JoinBy(" ")} {refs_args.JoinBy(" ")} {source_args.JoinBy(" ")} /out:""{assembly}""";
                    }
                    else
                        cmd = $@"""{Globals.csc}"" {common_args.JoinBy(" ")} {refs_args.JoinBy(" ")} {source_args.JoinBy(" ")} /out:""{assembly}""";

                    result.NativeCompilerReturnValue = dotnet.Run(cmd, build_dir, x => result.Output.Add(x));
                }
                else
                {
                    cmd = $@" {common_args.JoinBy(" ")} {refs_args.JoinBy(" ")} {source_args.JoinBy(" ")} /out:""{assembly}""";

                    result.NativeCompilerReturnValue = Globals.csc.Run(cmd, build_dir, x => result.Output.Add(x));
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
                    if (outAssembly != null)
                        File.Copy(assembly, outAssembly, true);

                    if (!IncludeDebugInformation)
                    {
                        if (outAssembly != null)
                            File.Copy(assembly, outAssembly, true);

                        return (File.ReadAllBytes(assembly), null);
                    }
                    else
                    {
                        if (outAssembly != null)
                        {
                            File.Copy(assembly, outAssembly, true);
                            File.Copy(assembly.ChangeExtension(".pdb"), outAssembly.ChangeExtension(".pdb"), true);
                        }

                        return (File.ReadAllBytes(assembly),
                                File.ReadAllBytes(assembly.ChangeExtension(".pdb")));
                    }
                }
                else
                {
                    if (result.Errors.IsEmpty())
                    {
                        // unknown error; e.g. invalid compiler params
                        result.Errors.Add(new CompilerError { ErrorText = "Unknown compiler error" });
                    }
                    throw CompilerException.Create(result.Errors, true, true);
                }
            }
            finally
            {
                build_dir.DeleteDir();
            }
        }
    }
}