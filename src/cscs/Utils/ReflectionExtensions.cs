using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace csscript
{
    public static class ReflectionExtensions
    {
        public static string Directory(this Assembly asm)
        {
            var file = asm.Location();
            if (file.IsNotEmpty())
                return Path.GetDirectoryName(file);
            else
                return "";
        }

        //to avoid throwing the exception
        public static string Location(this Assembly asm)
        {
            if (asm.IsDynamic())
            {
                string location = Environment.GetEnvironmentVariable("location:" + asm.GetHashCode());
                if (location == null)
                    return "";
                else
                    return location ?? "";
            }
            else
                return asm.Location;
        }

        public static string GetName(this Type type)
        {
            return type.GetTypeInfo().Name;
        }

        /// <summary>
        /// Creates instance of a class from underlying assembly.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
        /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>
        /// Instance of the 'Type'. Throws an ApplicationException if the instance cannot be created.
        /// </returns>
        public static object CreateObject(this Assembly asm, string typeName, params object[] args)
        {
            return CreateInstance(asm, typeName, args);
        }

        /// <summary>
        /// Creates instance of a Type from underlying assembly.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <param name="typeName">Name of the type to be instantiated. Allows wild card character (e.g. *.MyClass can be used to instantiate MyNamespace.MyClass).</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>
        /// Created instance of the type.
        /// </returns>
        /// <exception cref="System.Exception">Type " + typeName + " cannot be found.</exception>
        private static object CreateInstance(Assembly asm, string typeName, params object[] args)
        {
            //note typeName for FindTypes does not include namespace
            if (typeName == "*")
            {
                //instantiate the first type found (but not auto-generated types)
                //Ignore Roslyn internal type: "Submission#N"; real script class will be Submission#0+Script
                foreach (Type type in asm.GetTypes())
                {
                    bool isMonoInternalType = (type.FullName == "<InteractiveExpressionClass>");
                    bool isRoslynInternalType = (type.FullName.StartsWith("Submission#") && !type.FullName.Contains("+"));

                    if (!isMonoInternalType && !isRoslynInternalType)
                    {
                        return Activator.CreateInstance(type, args);
                    }
                }
                return null;
            }
            else
            {
                var name = typeName.Replace("*.", "");

                Type[] types = asm.GetTypes()
                                  .Where(t => t.FullName.None(char.IsDigit)
                                              && (t.FullName == name
                                                    || t.FullName == ("Submission#0+" + name)
                                                    || t.Name == name))
                                      .ToArray();

                if (types.Length == 0)
                    throw new Exception("Type " + typeName + " cannot be found.");

                return Activator.CreateInstance(types.First(), args);
            }
        }

        internal static Type FirstUserTypeAssignableFrom<T>(this Assembly asm)
        {
            // exclude Roslyn internal types
            return asm
                .ExportedTypes
                .Where(t => t.FullName.None(char.IsDigit)           // 1 (yes Roslyn can generate class with this name)
                       && t.FullName.StartsWith("Submission#0+")  // Submission#0+Script
                          && !t.FullName.Contains("<<Initialize>>")) // Submission#0+<<Initialize>>d__0
                .FirstOrDefault(x => typeof(T).IsAssignableFrom(x));
        }

        public static bool IsDynamic(this Assembly asm)
        {
            //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
            //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
            return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
        }

        public static Exception CaptureExceptionDispatchInfo(this Exception ex)
        {
            try
            {
                // on .NET 4.5 ExceptionDispatchInfo can be used
                // ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                typeof(Exception).GetMethod("PrepForRemoting", BindingFlags.NonPublic | BindingFlags.Instance)
                                 .Invoke(ex, new object[0]);
            }
            catch { }
            return ex;
        }
    }
}