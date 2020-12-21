using System;
using System.Collections.Generic;
using static System.Environment;
using System.Linq;
using System.Reflection;
using csscript;
using CSScriptLib;

namespace CSScripting.CodeDom
{
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

            // only dotnet has a distinctive error message that separates "info" and "error" section.
            // It is particularly important to process only the "error" section as dotnet compiler prints
            // the same errors in both of these sections.

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
                        var error = CompilerError.Parse(line);
                        if (error != null)
                            Errors.Add(error);
                    }
                }
                else
                {
                    if (line.IsNotEmpty())
                    {
                        var error = CompilerError.Parse(line);
                        if (error != null)
                            Errors.Add(error);
                    }
                }
            }

            // if (Errors.IsEmpty() && !Output.IsEmpty())
            //     Errors.Add(new CompilerError
            //     {
            //         ErrorText = NewLine + Output.Where(x => x.IsNotEmpty()).JoinBy(NewLine),
            //         ErrorNumber = "CS0000"
            //     });
        }
    }
}