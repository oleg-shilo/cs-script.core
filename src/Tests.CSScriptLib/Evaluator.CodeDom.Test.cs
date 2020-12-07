using System;
using System.IO;
using System.Reflection;
using csscript;
using CSScripting;
using CSScriptLib;
using Xunit;

namespace EvaluatorTests
{
    public class CodeDom
    {
        [Fact]
        public void call_LoadMethod()
        {
            CSScript.EvaluatorConfig.DebugBuild = true;
            CodeDomEvaluator.CompileOnServer = true;

            dynamic script = CSScript.CodeDomEvaluator
                                      .LoadMethod(@"public object func()
                                                {
                                                    return new[] {0,5};
                                                }");

            var result = (int[])script.func();

            Profiler.Stopwatch.Start();
            script = CSScript.CodeDomEvaluator
                             .LoadMethod(@"public object func()
                                           {
                                               return 77;
                                           }");

            var resultsum = (int)script.func();

            var time = Profiler.Stopwatch.ElapsedMilliseconds;

            Assert.Equal(0, result[0]);
            Assert.Equal(5, result[1]);
        }

        [Fact]
        public void call_CompileMethod()
        {
            dynamic script = CSScript.RoslynEvaluator
                                     .CompileMethod(@"public object func() => new[] {0,5}; ")
                                     .CreateObject("*.DynamicClass");

            var result = (int[])script.func();

            Assert.Equal(0, result[0]);
            Assert.Equal(5, result[1]);
        }

        [Fact]
        public void referencing_script_types_from_another_script()
        {
            CSScript.EvaluatorConfig.DebugBuild = true;

            var code2 = @"using System;
                      using System.Collections.Generic;
                      using System.Linq;

                      public class Utils
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

            var info = new CompileInfo { RootClass = "script_a", AssemblyFile = "script_a_asm" };

            CSScript.RoslynEvaluator.CompileCode(code2, info);

            dynamic script = CSScript.Evaluator
                                     .ReferenceAssembly(info.AssemblyFile)
                                     .CompileMethod(@"using static script_a;
                                                  Utils Test()
                                                  {
                                                      return new Utils();
                                                  }")
                                     .CreateObject("*");

            object utils = script.Test();

            Assert.Equal("script_a+Utils", utils.GetType().ToString());
        }

        [Fact]
        public void use_interfaces_between_scripts()
        {
            IPrinter printer = CSScript.Evaluator
                                       .ReferenceAssemblyOf<IPrinter>()
                                       .LoadCode<IPrinter>(@"using System;
                                                         public class Printer : IPrinter
                                                         {
                                                            public void Print()
                                                                => Console.Write(""Printing..."");
                                                         }");

            dynamic script = CSScript.Evaluator
                                     .ReferenceAssemblyOf<IPrinter>()
                                     .LoadMethod(@"void Test(IPrinter printer)
                                               {
                                                   printer.Print();
                                               }");
            script.Test(printer);
        }

        [Fact(Skip = "VB is not supported yet")]
        public void VB_Generic_Test()
        {
            Assembly asm = CSScript.Evaluator
                                   .CompileCode(@"' //css_ref System
                                              Imports System
                                              Class Script

                                                Function Sum(a As Integer, b As Integer)
                                                    Sum = a + b
                                                End Function

                                            End Class");

            dynamic script = asm.CreateObject("*");
            var result = script.Sum(7, 3);
        }

        [Fact]
        public void Issue_185_Referencing()
        {
            CSScript.EvaluatorConfig.DebugBuild = true;

            var root_class_name = $"script_{System.Guid.NewGuid()}".Replace("-", "");

            var info = new CompileInfo { RootClass = root_class_name, PreferLoadingFromFile = true };

            var printer_asm = CSScript.Evaluator
                                      .CompileCode(@"using System;
                                                 public class Printer
                                                 {
                                                     public void Print() => Console.Write(""Printing..."");
                                                 }", info);

            dynamic script = CSScript.Evaluator
                                     .ReferenceAssembly(printer_asm)
                                     .LoadMethod($"using static {root_class_name};" + @"
                                               void Test()
                                               {
                                                   new Printer().Print();
                                               }");
            script.Test();
        }
    }
}