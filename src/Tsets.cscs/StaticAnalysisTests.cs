using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Dia2Lib;
using Mono.Reflection;
using Xunit;
using static System.Reflection.BindingFlags;

public class StaticAnalysisTests
{
    [Fact]
    public void Find_DeadCode()
    {
        // inconclusive, too many false positives
        var dead_methods = typeof(csscript.CSExecutor).Assembly
                                                      .GetUnReferencedMethods()
                                                      .Select(x => x.FullName())
                                                      .Select(x => x.Replace(".get_", ".").Replace(".set_", "."))
                                                      .Distinct()
                                                      .ToList();

        dead_methods.ForEach(x => Trace.WriteLine(x));
    }
}