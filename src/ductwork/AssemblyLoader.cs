using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ductwork;

public static class AssemblyLoader
{
    public static Assembly Load(string path, IEnumerable<string>? searchPaths = null)
    {
        return new LoaderContext(path, searchPaths).Load();
    }

    private class LoaderContext
    {
        private readonly string _assemblyPath;

        public LoaderContext(string assemblyPath, IEnumerable<string>? searchRoots = null)
        {
            _assemblyPath = new[] { assemblyPath }
                                .Concat(
                                    (searchRoots ?? [])
                                    .Select(searchRoot => Path.Combine(searchRoot, assemblyPath)))
                                .Select(Path.GetFullPath)
                                .Where(File.Exists)
                                .FirstOrDefault()
                            ?? assemblyPath;
        }

        public Assembly Load()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            try
            {
                var assembly = Assembly.LoadFile(_assemblyPath);
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

            if (assembly is not null)
            {
                return assembly;
            }

            var filename = $"{name.Split(',').First()}.dll";
            var lookupPaths = new[]
            {
                Path.GetDirectoryName(args.RequestingAssembly?.Location) ?? string.Empty,
                Path.GetFullPath("./"),
            };

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