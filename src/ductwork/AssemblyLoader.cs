using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ductwork;

public static class AssemblyLoader
{
    public static Assembly Load(string path)
    {
        return new LoaderContext(path).Load();
    }

    private class LoaderContext
    {
        private readonly string _path;
        private readonly string _directory;

        public LoaderContext(string path)
        {
            _path = path;
            _directory = Path.GetDirectoryName(path) ?? "./";
        }

        public Assembly Load()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            try
            {
                var assembly = Assembly.LoadFile(_path);
                // Resolve any dependencies by forcing types to load.
                assembly.GetTypes();
                return assembly;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        private Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var name = args.Name;

            if (name.Contains(".resources"))
            {
                return null;
            }

            var assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.FullName == name);
            if (assembly != null)
            {
                return assembly;
            }

            var filename = $"{name.Split(',').First()}.dll";
            var lookupPaths = new[] {Path.GetFullPath("./"), _directory};

            foreach (var lookupPath in lookupPaths)
            {
                var path = Path.Join(lookupPath, filename);
                try
                {
                    return Assembly.LoadFrom(path);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return null;
        }
    }
}