using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;
using CSScriptLib;

public interface ICalc
{
    int Sum(int a, int b);
}

public interface ICalc2
{
    int Sum(int a, int b);

    int Div(int a, int b);
}

public class Samples
{
    public static void LoadCode()
    {
        dynamic calc = CSScript.Evaluator
                               .LoadCode(
                                   @"using System;
                                     public class Script : ICalc
                                     {
                                         public int Sum(int a, int b)
                                         {
                                             return a+b;
                                         }
                                     }");
        int result = calc.Sum(1, 2);
    }

    public static void LoadFile()
    {
        var script = Path.GetTempFileName();
        try
        {
            File.WriteAllText(script, @"using System;
                                        public class Script : ICalc
                                        {
                                            public int Sum(int a, int b)
                                            {
                                                return a+b;
                                            }
                                        }");

            dynamic calc = CSScript.Evaluator.LoadFile(script);

            int result = calc.Sum(1, 2);
        }
        finally
        {
            File.Delete(script);
        }
    }

    public static void LoadAndUnload()
    {
        var asmFile = Path.GetTempFileName();
        try
        {
            CSScript.Evaluator.CompileAssemblyFromCode(
                               @"using System;
                                 public class Script
                                 {
                                     public int Sum(int a, int b)
                                     {
                                         return a+b;
                                     }
                                 }", asmFile);

            var asmContext = new AssemblyLoadContext(null, isCollectible: true);
            asmContext.Unloading += x => Console.WriteLine("Unloading script");

            dynamic script = asmContext.LoadFromAssemblyPath(asmFile).CreateInstance("css_root+Script");

            int result = script.Sum(1, 2);

            asmContext.Unload();
        }
        finally
        {
            try
            {
                File.Delete(asmFile);
            }
            catch { }
        }
    }

    public static void LoadMethod()
    {
        dynamic script = CSScript.RoslynEvaluator
                                 .LoadMethod(
                                     @"int Product(int a, int b)
                                       {
                                           return a * b;
                                       }");

        int result = script.Product(3, 2);
    }

    public static void LoadMethodWithInterface()
    {
        ICalc2 script = CSScript.RoslynEvaluator
                                .LoadMethod<ICalc2>(
                                    @"public int Sum(int a, int b)
                                      {
                                          return a + b;
                                      }
                                      public int Div(int a, int b)
                                      {
                                          return a/b;
                                      }");
        int result = script.Sum(15, 3);
    }

    public static void CreateDelegate()
    {
        var log = CSScript.RoslynEvaluator
                          .CreateDelegate(@"void Log(string message)
                                            {
                                                Console.WriteLine(message);
                                            }");

        log("Test message");
    }

    public static void LoadCodeWithInterface()
    {
        string code = @"
                using System;
                public class Script : ICalc
                {
                    public int Sum(int a, int b)
                    {
                        return a + b;
                    }
                }";

        var script = CSScript.Evaluator.LoadCode<ICalc>(code);

        int result = script.Sum(13, 2);
    }

    public static void CompileCode()
    {
        var info = new CompileInfo { RootClass = "printer_script", AssemblyFile = "script.dll" };

        var printer_asm = CSScript.Evaluator
                                  .CompileCode(
                                      @"using System;
                                        public class Printer
                                        {
                                            public static void Print() =>
                                                Console.WriteLine(""Printing..."");
                                        }", info);

        printer_asm
            .GetType("printer_script+Printer")
            .GetMethod("Print")
            .Invoke(null, null);
    }

    public static void ScriptReferencingScript()
    {
        var info = new CompileInfo { RootClass = "printer_script" };

        var printer_asm = CSScript.Evaluator
                                  .CompileCode(
                                      @"using System;
                                        public class Printer
                                        {
                                            public void Print() =>
                                                Console.WriteLine(""Printing..."");
                                        }", info);

        dynamic script = CSScript.Evaluator
                                 .ReferenceAssembly(printer_asm)
                                 .LoadMethod(@"using static printer_script;
                                               void Test()
                                               {
                                                   new Printer().Print();
                                               }");
        script.Test();
    }

    public static void LoadDelegate()
    {
        try
        {
            var Product = CSScript.Evaluator
                                  .LoadDelegate<Func<int, int, int>>(
                                      @"int Product(int a, int b)
                                            {
                                                return a * b;
                                            }");

            int result = Product(3, 2);
        }
        catch (NotImplementedException ex)
        {
            // This method is only implemented for CS-Script .NET but not .NET Core
            // You may want to consider using interfaces with LoadCode/LoadMethod or use CreateDelegate instead.
        }
    }

    public static void Referencing()
    {
        string code = @"
                using System;
                using System.Xml;

                public class Script : ICalc
                {
                    public int Sum(int a, int b)
                    {
                        return a + b;
                    }
                }";

        var script = CSScript.Evaluator
                             .ReferenceAssembliesFromCode(code)
                             .ReferenceAssembly(Assembly.GetExecutingAssembly())
                             .ReferenceAssembly(Assembly.GetExecutingAssembly().Location)
                             .ReferenceAssemblyByName("System")
                             .ReferenceAssemblyByNamespace("System.Xml")
                             .TryReferenceAssemblyByNamespace("Fake.Namespace", out var resolved)
                             .ReferenceAssemblyOf(new Samples())
                             .ReferenceAssemblyOf<XDocument>()
                             .ReferenceDomainAssemblies()
                             .LoadCode<ICalc>(code);

        int result = script.Sum(13, 2);
    }
}