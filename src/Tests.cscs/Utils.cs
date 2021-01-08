using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Reflection;
using static System.Reflection.BindingFlags;

static class Extensions
{
    public static string Run(this string exe, string args = null, string dir = null)
    {
        var process = new Process();

        process.StartInfo.FileName = exe;
        process.StartInfo.Arguments = args;
        process.StartInfo.WorkingDirectory = dir;

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }
}

public static class StaticAnalyzer // inconclusive, too many false positives
{
    public static string FullName(this MethodInfo method)
        => $"{method.DeclaringType.FullName}.{method.Name}";

    public static void VisitMethodCalls(this Assembly asm, Func<MethodInfo, MethodInfo, bool> visit)
    {
        var methods = asm.GetTypes()
                         .SelectMany(x => x.GetMethods(Public | NonPublic | Instance | Static)
                                           .Where(y => y.DeclaringType.Assembly == asm));

        foreach (MethodInfo caller in methods)
            foreach (Instruction instruction in caller.GetInstructions())
            {
                var calledMethod = instruction.Operand as MethodInfo;
                if (calledMethod != null)
                {
                    if (!visit(caller, calledMethod))
                        break;
                }
            }
    }

    public static IEnumerable<MethodInfo> GetUnReferencedMethods(this Assembly asm)
    {
        var methods = asm.GetTypes()
                         .Where(x => !x.Name.Contains("_AnonymousType") &&
                                     !x.Name.Contains("d_") &&
                                     !x.Name.Contains("b_") &&
                                     !x.Name.Contains("_DisplayClass"))
                         .Where(x => !x.IsPublic)
                         .SelectMany(x => x.GetMethods(Public | NonPublic | Instance | Static)
                                           .Where(y => y.DeclaringType.Assembly == asm))
                         .Where(x => !x.Name.Contains("b_") &&
                                     !x.Name.Contains("g_"))
                         // .Select(x => new { Name = x.FullName, Method = x })
                         .ToArray();
        var notCalled = methods.ToList();

        foreach (MethodInfo caller in methods)
        {
            if (notCalled.Any() == false)
                break;
            try
            {
                foreach (Instruction instruction in caller.GetInstructions())
                {
                    var calledMethod = instruction.Operand as MethodInfo;
                    if (calledMethod != null)
                    {
                        if (notCalled.Contains(calledMethod))
                            notCalled.Remove(calledMethod);
                    }
                }
            }
            catch { }
        }

        return notCalled;
    }
}