using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

#if class_lib

namespace CSScriptLib
#else

namespace csscript
#endif
{
    public partial class CSScript
    {
        static internal string DynamicWrapperClassName = "DynamicClass";
        static internal string RootClassName = "css_root";
        // Roslyn still does not support anything else but `Submission#0` (17 Jul 2019)
        // [update] Roslyn now does support alternative class names (1 Jan 2020)
    }

    /// <summary>
    /// Various Reflection extensions
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Returns directory where the specified assembly file is.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <returns></returns>
        public static string Directory(this Assembly asm)
        {
            var file = asm.Location();
            if (file.IsNotEmpty())
                return Path.GetDirectoryName(file);
            else
                return "";
        }

        /// <summary>
        /// Returns location of the specified assembly. Avoids throwing an exception in case
        /// of dynamic assembly.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
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
                //instantiate the user first type found (but not auto-generated types)
                //Ignore Roslyn internal root type: "Submission#0"; real script class will be Submission#0+Script

                var firstUserTypes = asm.GetTypes()
                                        .FirstOrDefault(x => x.FullName.StartsWith(CSScript.RootClassName) &&
                                                             x.FullName != CSScript.RootClassName);

                if (firstUserTypes != null)
                    return Activator.CreateInstance(firstUserTypes, args);

                return null;
            }
            else
            {
                var name = typeName.Replace("*.", "");

                Type[] types = asm.GetTypes()
                                  .Where(t => (t.FullName == name
                                               || t.FullName == ($"{CSScript.RootClassName}+{name}")
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
                .Where(t => t.FullName.StartsWith($"{CSScript.RootClassName}+")  // Submission#0+Script
                            && !t.FullName.Contains("<<Initialize>>")) // Submission#0+<<Initialize>>d__0

                .FirstOrDefault(x => typeof(T).IsAssignableFrom(x));
        }

        /// <summary>
        /// Determines whether the assembly is dynamic.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <returns>
        ///   <c>true</c> if the specified asm is dynamic; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDynamic(this Assembly asm)
        {
            //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
            //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
            return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
        }
    }
}