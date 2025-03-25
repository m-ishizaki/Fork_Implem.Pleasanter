using Implem.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
namespace Implem.Libraries.Plugins
{
    public class ExtendedLibraryLoadContext : AssemblyLoadContext
    {
        public string PluginDirectory { get; init; }
        public bool IsSearchAllDirectories { get; init; }

        public ExtendedLibraryLoadContext(string pluginDirectory) : this(pluginDirectory, true)
        {
        }

        public ExtendedLibraryLoadContext(string pluginDirectory, bool isSearchAllDirectories)
        {
            PluginDirectory = pluginDirectory;
            IsSearchAllDirectories = isSearchAllDirectories;
        }

        private IEnumerable<Assembly> Load(string[] files)
        {
            var dllFiles = files.Select(file => Path.GetFullPath(file));
            var loaded = new List<Assembly>();
            foreach (var dllFile in dllFiles)
            {
                var assembly = LoadFromAssemblyPath(dllFile);
                loaded.Add(assembly);
            }
            return loaded.ToArray();
        }

        public IEnumerable<Assembly> Load()
            => Load(Directory
                .GetFiles(PluginDirectory, "*.dll", IsSearchAllDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .ToArray());

        public static IEnumerable<ExtendedLibraryLoadContext> LoadExtensions(string pluginDirectory)
        {
            if (!Directory.Exists(pluginDirectory)) return Array.Empty<ExtendedLibraryLoadContext>();
            var directories = Directory
                .GetDirectories(pluginDirectory)
                .Where(directory => Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories).Any())
                .ToArray();
            var contexts = directories.Select(directory => new ExtendedLibraryLoadContext(directory)).ToArray();
            foreach (var context in contexts)
                context.Load();

            if (Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly).Any())
            {
                var context = new ExtendedLibraryLoadContext(pluginDirectory, false);
                context.Load();
                contexts = new[] { context }.Concat(contexts).ToArray();
            }

            return contexts;
        }
    }
}
