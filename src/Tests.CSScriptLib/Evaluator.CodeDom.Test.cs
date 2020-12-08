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
            dynamic script = CSScript.CodeDomEvaluator
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
            var info = new CompileInfo { AssemblyFile = "utils_asm" };

            try
            {
                var utils_code = @"using System;
                               using System.Collections.Generic;
                               using System.Linq;

                               public class Utils
                               {
                                   public class Printer { }

                                   static void Main(string[] args)
                                   {
                                       var x = new List<int> {1, 2, 3, 4, 5};
                                       var y = Enumerable.Range(0, 5);

                                       x.ForEach(Console.WriteLine);
                                       var z = y.First();
                                       Console.WriteLine(z);
                                   }
                               }";

                var asm = CSScript.CodeDomEvaluator.CompileCode(utils_code, info);

                dynamic script = CSScript.CodeDomEvaluator
                                         .ReferenceAssembly(info.AssemblyFile)
                                         .CompileMethod(@"public Utils NewUtils() => new Utils();
                                                      public Utils.Printer NewPrinter() => new Utils.Printer();")
                                         .CreateObject("*");

                object utils = script.NewUtils();
                object printer = script.NewPrinter();

                Assert.Equal("Utils", utils.GetType().ToString());
                Assert.Equal("Utils+Printer", printer.GetType().ToString());
            }
            finally
            {
                info.AssemblyFile.FileDelete(rethrow: false); // assembly is locked so only showing the intention
            }
        }

        [Fact]
        public void use_interfaces_between_scripts()
        {
            IPrinter printer = CSScript.CodeDomEvaluator
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
            Assembly asm = CSScript.CodeDomEvaluator
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
    }
}