using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace compile_server
{
    public class SimpleAsmProbing : IDisposable
    {
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing) => Uninit();

        ~SimpleAsmProbing() => Dispose(false);

        public SimpleAsmProbing()
        {
        }

        public static SimpleAsmProbing For(params string[] probingDirs) => new SimpleAsmProbing(probingDirs);

        public SimpleAsmProbing(params string[] probingDirs) => Init(probingDirs);

        public static bool InMemoryLoading = true;
        static bool initialized = false;
        static string[] probingDirs = new string[0];

        public void Init(params string[] probingDirs)
        {
            SimpleAsmProbing.probingDirs = probingDirs;
            if (!initialized)
            {
                initialized = true;
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        public void Uninit()
        {
            if (initialized)
            {
                initialized = false;
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        static Dictionary<string, Assembly> cache = new();

        Assembly LoadAssembly(string file) => InMemoryLoading ? Assembly.Load(File.ReadAllBytes(file)) : Assembly.LoadFile(file);

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var shortName = args.Name.Split(',').First().Trim();

            if (cache.ContainsKey(shortName))
                return cache[shortName];

            cache[shortName] = null; // this will prevent reentrance an cercular calls

            foreach (string dir in probingDirs)
            {
                try
                {
                    string file = Path.Combine(dir, args.Name.Split(',').First().Trim() + ".dll");
                    if (File.Exists(file))
                        return (cache[shortName] = LoadAssembly(file));
                }
                catch { }
            }

            try
            {
                return (cache[shortName] = Assembly.LoadFrom(shortName)); // will try to load by the asm file name without the path
            }
            catch
            {
            }
            return null;
        }
    }
}