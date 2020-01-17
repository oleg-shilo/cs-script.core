using System.Reflection;
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

class ScriptHost
{
    void LoadCode()
    {
        ICalc calc = CSScript.Evaluator
                             .LoadCode<ICalc>(
                                 @"using System;
                                   public class Script
                                   {
                                       public int Sum(int a, int b)
                                       {
                                           return a+b;
                                       }
                                   }");
        int result = calc.Sum(1, 2);
    }

    void LoadMethod()
    {
        dynamic script = CSScript.RoslynEvaluator
                                 .LoadMethod(
                                     @"int Product(int a, int b)
                                       {
                                           return a * b;
                                       }");

        int result = script.Product(3, 2);
    }

    void LoadMethodWithInterface()
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

    void CreateDelegate()
    {
        var log = CSScript.RoslynEvaluator
                          .CreateDelegate(@"void Log(string message)
                                            {
                                                Console.WriteLine(message);
                                            }");

        log("Test message");
    }

    void LoadCodeWithInterface()
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

    void Referencing()
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
                             .ReferenceAssembly(this.GetType().Assembly.Location)
                             .ReferenceAssemblyByName("System")
                             .ReferenceAssemblyByNamespace("System.Xml")
                             .TryReferenceAssemblyByNamespace("Fake.Namespace", out var resolved)
                             .ReferenceAssemblyOf(this)
                             .ReferenceAssemblyOf<XDocument>()
                             .ReferenceDomainAssemblies()
                             .LoadMethod<ICalc>(code);

        int result = script.Sum(13, 2);
    }
}