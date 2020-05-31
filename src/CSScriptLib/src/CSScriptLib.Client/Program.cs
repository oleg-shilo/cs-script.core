using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using CSScriptLib;

// using Newtonsoft.Json.Linq;

namespace EvalTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CSScript.EvaluatorConfig.DebugBuild = true;
            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;

            dynamic script = CSScript.RoslynEvaluator
                                     .ReferenceAssembly("System.ValueTuple")
                                     .LoadMethod(@"void Hello()
                                                   {
                                                       (int count, double sum) Test()
                                                       {
                                                           Console.WriteLine(""Local method..."");
                                                           return (1, 2);
                                                       }

                                                       var t = Test();
                                                       Console.WriteLine(""Hello ! - ""+t.count);
                                                   }");

            script.Hello();
            // =========================
            var ttt = "".Cast<char>();
            // CSScript.EvaluatorConfig.DebugBuild = true;

            // Test.ReferencingPackagesCode(); //return;
            Client.Test.CompileCode();
            Client.Test.CompileMethod();
            Client.Test.CompileCSharp_7();
            Client.Test.CompileDelegate();
            Client.Test.CompileDelegate1();
            Client.Test.LoadCode();
            Client.Test.LoadCode2();
            // Test.CrossReferenceCode();

            // dynamic func1 = CSScript.Evaluator.LoadMethod(
            //       @"public object Func()
            //         {
            //             return 1;
            //         }");
            // Console.WriteLine("Result: " + func1.Func().ToString());

            // dynamic func2 = CSScript.Evaluator.LoadMethod(@"
            //         public object Func()
            //         {
            //             return EvalTest.Program.CallMe();
            //         }");
            // Console.WriteLine("Result: " + func2.Func().ToString());

            dynamic func3 = CSScript.Evaluator.LoadMethod(@"
                    using EvalTest;
                    public object Func()
                    {
                        return 3;
                    }");
            Console.WriteLine("Result: " + func3.Func().ToString());
        }

        public static int CallMe() => 2;
    }
}