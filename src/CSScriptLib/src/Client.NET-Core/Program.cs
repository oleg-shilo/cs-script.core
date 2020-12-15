using CSScriptLib;
using System;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            Test_CodeDom();
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.Restart();
            Test_CodeDom();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        static void Test_CodeDom()
        {
            CSScript.EvaluatorConfig.DebugBuild = true;
            CodeDomEvaluator.CompileOnServer = true;

            dynamic script = CSScript.CodeDomEvaluator
                                     .LoadMethod(@"public object func()
                                                   {
                                                       return new[] {0,5};
                                                   }");

            var result = (int[])script.func();
            // Console.WriteLine(result);
        }
    }
}