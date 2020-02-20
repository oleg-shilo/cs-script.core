extern alias lib;

using System;
using System.IO;
using csscript;
using CSScriptLib;
using Xunit;
using css = lib::CSScriptLib;

public interface IPrinter
{
    void Print();
}

public class EvaluatorTests
{
    [Fact]
    public void Issue_4()
    {
        var test = css.CSScript.Evaluator.CompileMethod("public object func() { return new[] {0,2,3,5}; }")
                                                        .CreateObject("*.DynamicClass");

        dynamic script = css.CSScript.Evaluator
                                      .LoadMethod(@"public object func()
                                                                     {
                                                                         return new[] {0,2,3,5};
                                                                     }");

        var result = script.func();
    }

    [Fact]
    public void Issue_185()
    {
        lib::CSScriptLib.CSScript.EvaluatorConfig.DebugBuild = true;

        var code2 = @"
     using System;
     using System.Collections.Generic;
     using System.Linq;

     public class Usings
     {
         static void Main(string[] args)
         {
             var x = new List<int> {1, 2, 3, 4, 5};
             var y = Enumerable.Range(0, 5);

             x.ForEach(Console.WriteLine);
             var z = y.First();
             Console.WriteLine(z);
         }
     }";

        var info = new css.CompileInfo { RootClass = "code2", AssemblyFile = "code2" };
        css.CSScript.Evaluator.CompileCode(code2, info);

        dynamic script = css.CSScript.Evaluator
                                     .ReferenceAssembly(info.AssemblyFile)
                                     .CompileMethod(@"using static code2;
                                             Usings Test()
                                             {
                                                 return new Usings();
                                             }")
                                     .CreateObject("*");
        script.Test();
    }

    [Fact]
    public void Issue_185_Interfaces()
    {
        IPrinter printer = css.CSScript.Evaluator
                                       .ReferenceAssemblyOf<IPrinter>()
                                       .LoadCode<IPrinter>(@"using System;
                                                             public class Printer : IPrinter
                                                             {
                                                                public void Print()
                                                                    => Console.Write(""Printing..."");
                                                             }");

        dynamic script = css.CSScript.Evaluator
                                     .ReferenceAssemblyOf<IPrinter>()
                                     .LoadMethod(@"void Test(IPrinter printer)
                                                   {
                                                       printer.Print();
                                                   }");
        script.Test(printer);
    }

    [Fact]
    public void Issue_185_Referencing()
    {
        lib::CSScriptLib.CSScript.EvaluatorConfig.DebugBuild = true;

        var root_class_name = $"script_{System.Guid.NewGuid()}".Replace("-", "");

        var info = new css.CompileInfo { RootClass = root_class_name, PreferLoadingFromFile = true };

        var printer_asm = css.CSScript.Evaluator
                                      .CompileCode(@"using System;
                                                     public class Printer
                                                     {
                                                         public void Print() => Console.Write(""Printing..."");
                                                     }", info);

        dynamic script = css.CSScript.Evaluator
                                     .ReferenceAssembly(printer_asm)
                                     .LoadMethod($"using static {root_class_name};" + @"
                                                   void Test()
                                                   {
                                                       new Printer().Print();
                                                   }");
        script.Test();
    }

    [Fact]
    //  Learning to use CS-Script in a NET core application #8
    public void Issue_8()
    {
        var script = css.CSScript.Evaluator
                                  .CompileMethod(@"int add(int a, int b)
                                            {
                                                return a + b;
                                            }");

        dynamic calc = css.CSScript.Evaluator
                                    .LoadMethod(@"int add(int a, int b)
                                                  {
                                                      return a + b;
                                                  }");
        var result = calc.add(1, 3);

        var iCalc = css.CSScript.Evaluator
                       .LoadMethod<ICalc>(@"using System;
                                            public int Sum(int a, int b)
                                            {
                                                return a + b;
                                            }");
        result = iCalc.Sum(13, 3);

        var ccc = css.CSScript.Evaluator.LoadCode<ICalc>(@"using System;
                public class Script : ICalc
                {
                    public int Sum(int a, int b)
                    {
                        return a + b;
                    }
                }");
        result = ccc.Sum(13, 2);

        // LoadDelegate is not implemented for .NET Core
        // Func<int, int, int> add = css.CSScript.Evaluator
        //                                        .LoadDelegate<Func<int, int, int>>(
        //                                            @"int add(int a, int b)
        //                                               {
        //                                                   return a + b;
        //                                               }");
        // result = add(4, 5);
    }
}

public interface ICalc
{
    int Sum(int a, int b);
}